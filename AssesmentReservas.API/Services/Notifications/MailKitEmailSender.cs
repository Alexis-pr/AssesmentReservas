using AssesmentReservas.API.Interfaces.Notifications;
using AssesmentReservas.API.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AssesmentReservas.API.Services.Notifications;

/// <summary>Envío de correo vía SMTP usando MailKit (MailHog en desarrollo).</summary>
public class MailKitEmailSender : IEmailSender
{
    private readonly MailSettings _settings;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptions<MailSettings> settings, ILogger<MailKitEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string? toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        var secureOption = _settings.UseSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;
        await client.ConnectAsync(_settings.Host, _settings.Port, secureOption, ct);

        if (!string.IsNullOrWhiteSpace(_settings.User))
            await client.AuthenticateAsync(_settings.User, _settings.Password ?? string.Empty, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Correo enviado a {Email}: {Subject}", toEmail, subject);
    }
}
