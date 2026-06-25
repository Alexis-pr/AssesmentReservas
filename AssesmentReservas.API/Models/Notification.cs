using System.ComponentModel.DataAnnotations;
using AssesmentReservas.API.Enums;

namespace AssesmentReservas.API.Models;

/// <summary>Notificación in-app. Las de correo se despachan vía MailKit (no se persisten aquí).</summary>
public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;
    public ApplicationUser? User { get; set; }

    public NotificationType Type { get; set; }

    [MaxLength(150)]
    public string Title { get; set; } = default!;

    [MaxLength(1000)]
    public string Message { get; set; } = default!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
