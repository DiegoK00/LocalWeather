using WeatherDigest.Models;

namespace WeatherDigest.Services;

public interface IWeatherService
{
    /// <summary>
    /// Recupera le previsioni orarie da ciascun modello configurato e ne calcola
    /// la media ora per ora (temperatura in °C e precipitazioni in mm).
    /// </summary>
    Task<WeatherDigestResult> GetAveragedForecastAsync(CancellationToken cancellationToken);
}
