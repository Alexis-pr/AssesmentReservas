namespace AssesmentReservas.API.Interfaces.Notifications;

/// <summary>Envío de correos transaccionales.</summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string? toName, string subject, string htmlBody, CancellationToken ct = default);
}
