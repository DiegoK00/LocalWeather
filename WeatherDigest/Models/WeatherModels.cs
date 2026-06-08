using System.Text.Json.Serialization;

namespace WeatherDigest.Models;

/// <summary>Media oraria tra i modelli, per una singola ora.</summary>
public sealed record HourlyAverage(
    DateTime Hour,
    double TemperatureC,
    double PrecipitationMm,
    int SourceCount);

/// <summary>Sintesi di un giorno calcolata a partire dalle ore.</summary>
public sealed record DailySummary(
    DateOnly Date,
    double MinTemperatureC,
    double MaxTemperatureC,
    double TotalPrecipitationMm,
    IReadOnlyList<HourlyAverage> Hours);

/// <summary>Risultato completo: ore mediate + sintesi giornaliere.</summary>
public sealed record WeatherDigestResult(
    string LocationName,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<string> Models,
    IReadOnlyList<DailySummary> Days);

// --- DTO della risposta Open-Meteo (una chiamata per modello) ---

internal sealed record OpenMeteoResponse
{
    [JsonPropertyName("hourly")]
    public OpenMeteoHourly? Hourly { get; init; }
}

internal sealed record OpenMeteoHourly
{
    [JsonPropertyName("time")]
    public string[] Time { get; init; } = [];

    [JsonPropertyName("temperature_2m")]
    public double?[] Temperature2m { get; init; } = [];

    [JsonPropertyName("precipitation")]
    public double?[] Precipitation { get; init; } = [];
}
