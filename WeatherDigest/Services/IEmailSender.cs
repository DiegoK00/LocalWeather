namespace WeatherDigest.Services;

public interface IEmailSender
{
    /// <summary>Invia un'email con un singolo allegato PDF.</summary>
    Task SendWithPdfAsync(
        string subject,
        string body,
        string attachmentFileName,
        byte[] pdfBytes,
        CancellationToken cancellationToken);
}
