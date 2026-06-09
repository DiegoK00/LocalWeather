using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherDigest.Models;
using WeatherDigest.Options;

namespace WeatherDigest.Services;

/// <summary>
/// Interroga Open-Meteo una volta per ciascun modello e media i risultati ora per ora.
/// Open-Meteo è gratuita e senza API key per uso non commerciale.
/// </summary>
public sealed class OpenMeteoWeatherService(
    HttpClient httpClient,
    IOptions<WeatherOptions> options,
    ILogger<OpenMeteoWeatherService> logger) : IWeatherService
{
    private readonly WeatherOptions _options = options.Value;

    public async Task<WeatherDigestResult> GetAveragedForecastAsync(CancellationToken cancellationToken)
    {
        // timestamp ISO (Europe/Rome) -> letture di temperatura/precipitazioni dei vari modelli
        var byHour = new SortedDictionary<DateTime, (List<double> Temps, List<double> Precs)>();

        foreach (var model in _options.Models)
        {
            var response = await FetchModelAsync(model, cancellationToken);
            if (response?.Hourly is null)
            {
                logger.LogWarning("Nessun dato orario dal modello {Model}", model);
                continue;
            }

            MergeModel(response.Hourly, byHour);
        }

        if (byHour.Count == 0)
            throw new InvalidOperationException("Nessun modello ha restituito dati orari.");

        // Prima media: per ogni ora, media tra i modelli
        var hours = byHour
            .Where(kvp => kvp.Value.Temps.Count > 0)
            .Select(kvp => new HourlyAverage(
                Hour: kvp.Key,
                TemperatureC: Math.Round(kvp.Value.Temps.Average(), 1),
                PrecipitationMm: Math.Round(kvp.Value.Precs.DefaultIfEmpty(0).Average(), 2),
                SourceCount: kvp.Value.Temps.Count))
            .ToList();

        // Seconda media: raggruppa in fasce da 3h centrate (01-02-03 → 02:00, 04-05-06 → 05:00, ...)
        // La fascia è identificata dall'ora centrale: ((ora - 1) / 3) * 3 + 2
        // L'ora 00:00 appartiene all'ultima fascia del giorno precedente (22-23-00 → 23:00)
        var slots = hours
            .GroupBy(h =>
            {
                var slotHour = h.Hour.Hour == 0
                    ? h.Hour.Date.AddHours(-1)                          // 00:00 → 23:00 del giorno prima
                    : h.Hour.Date.AddHours(((h.Hour.Hour - 1) / 3) * 3 + 2);
                return slotHour;
            })
            .OrderBy(g => g.Key)
            .Select(g => new HourlyAverage(
                Hour: g.Key,
                TemperatureC: Math.Round(g.Average(h => h.TemperatureC), 1),
                PrecipitationMm: Math.Round(g.Sum(h => h.PrecipitationMm), 2),
                SourceCount: (int)Math.Round(g.Average(h => h.SourceCount))))
            .ToList();

        var days = slots
            .GroupBy(h => DateOnly.FromDateTime(h.Hour))
            .OrderBy(g => g.Key)
            .Select(g => new DailySummary(
                Date: g.Key,
                MinTemperatureC: Math.Round(g.Min(h => h.TemperatureC), 1),
                MaxTemperatureC: Math.Round(g.Max(h => h.TemperatureC), 1),
                TotalPrecipitationMm: Math.Round(g.Sum(h => h.PrecipitationMm), 1),
                Hours: g.OrderBy(h => h.Hour).ToList()))
            .ToList();

        return new WeatherDigestResult(
            LocationName: _options.LocationName,
            GeneratedAt: DateTimeOffset.UtcNow,
            Models: _options.Models,
            Days: days);
    }

    private async Task<OpenMeteoResponse?> FetchModelAsync(string model, CancellationToken cancellationToken)
    {
        var url =
            $"/v1/forecast?latitude={_options.Latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={_options.Longitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&hourly=temperature_2m,precipitation" +
            $"&forecast_days={_options.ForecastDays}" +
            $"&timezone={Uri.EscapeDataString(_options.TimeZone)}" +
            $"&models={Uri.EscapeDataString(model)}";

        logger.LogInformation("Chiamo Open-Meteo per il modello {Model}", model);
        return await httpClient.GetFromJsonAsync<OpenMeteoResponse>(url, cancellationToken);
    }

    private static void MergeModel(
        OpenMeteoHourly hourly,
        SortedDictionary<DateTime, (List<double> Temps, List<double> Precs)> byHour)
    {
        for (var i = 0; i < hourly.Time.Length; i++)
        {
            if (!DateTime.TryParse(hourly.Time[i], CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var hour))
                continue;

            if (!byHour.TryGetValue(hour, out var bucket))
            {
                bucket = (new List<double>(), new List<double>());
                byHour[hour] = bucket;
            }

            if (i < hourly.Temperature2m.Length && hourly.Temperature2m[i] is { } t)
                bucket.Temps.Add(t);

            if (i < hourly.Precipitation.Length && hourly.Precipitation[i] is { } p)
                bucket.Precs.Add(p);
        }
    }
}
