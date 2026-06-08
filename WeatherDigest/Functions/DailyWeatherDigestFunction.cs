using System.Globalization;
using Microsoft.Extensions.Logging;
using WeatherDigest.Services;

namespace WeatherDigest.Functions;

public sealed class DailyWeatherDigestFunction(
    IWeatherService weatherService,
    IWeatherReportPdfBuilder pdfBuilder,
    IEmailSender emailSender,
    ILogger<DailyWeatherDigestFunction> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Avvio bollettino meteo giornaliero alle {Now:o}", DateTimeOffset.UtcNow);

        var result = await weatherService.GetAveragedForecastAsync(cancellationToken);
        var pdf = pdfBuilder.Build(result);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var fileName = $"bollettino-meteo-{today}.pdf";

        await emailSender.SendWithPdfAsync(
            subject: $"Bollettino meteo {result.LocationName} – {today}",
            body: $"In allegato il bollettino per {result.LocationName} " +
                  $"(media dei modelli: {string.Join(", ", result.Models)}).",
            attachmentFileName: fileName,
            pdfBytes: pdf,
            cancellationToken: cancellationToken);

        logger.LogInformation("Bollettino inviato ({Bytes} byte) per {Days} giorni.",
            pdf.Length, result.Days.Count);
    }
}
