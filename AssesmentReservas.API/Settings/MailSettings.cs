namespace AssesmentReservas.API.Settings;

/// <summary>Configuración SMTP para el despacho de correos vía MailKit.</summary>
public class MailSettings
{
    public const string SectionName = "Mail";

    public string Host { get; set; } = "mailhog";
    public int Port { get; set; } = 1025;
    public string? User { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; }

    public string FromEmail { get; set; } = "no-reply@assesmentreservas.local";
    public string FromName { get; set; } = "Assesment Reservas";
}
