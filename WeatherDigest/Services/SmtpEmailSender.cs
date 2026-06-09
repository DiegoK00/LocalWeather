using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using WeatherDigest.Options;

namespace WeatherDigest.Services;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendWithPdfAsync(
        string subject,
        string body,
        string attachmentFileName,
        byte[] pdfBytes,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.From));
        message.To.Add(MailboxAddress.Parse(_options.To));
        message.Subject = subject;

        var builder = new BodyBuilder { TextBody = body };
        builder.Attachments.Add(attachmentFileName, pdfBytes, new ContentType("application", "pdf"));
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var secureOption = _options.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.SslOnConnect;

        Console.WriteLine($"Connecting to SMTP server {_options.Host}:{_options.Port} with secure option {secureOption}");
        Console.WriteLine($"Authenticating with user {_options.User}");

        await client.ConnectAsync(_options.Host, _options.Port, secureOption, cancellationToken);
        await client.AuthenticateAsync(_options.User, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
