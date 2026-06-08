using WeatherDigest.Models;

namespace WeatherDigest.Services;

public interface IWeatherReportPdfBuilder
{
    /// <summary>Genera il bollettino PDF a partire dal risultato mediato.</summary>
    byte[] Build(WeatherDigestResult result);
}
