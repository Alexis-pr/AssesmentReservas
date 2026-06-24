using AssesmentReservas.API.DTOs.Notifications;
using AssesmentReservas.API.Enums;

namespace AssesmentReservas.API.Interfaces.Notifications;

/// <summary>Motor omnicanal: persiste notificación in-app y, opcionalmente, despacha correo.</summary>
public interface INotificationService
{
    Task NotifyAsync(string userId, NotificationType type, string title, string message,
        bool sendEmail = true, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(string userId, bool unreadOnly = false, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
    Task MarkAsReadAsync(int id, string userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
}
