using AssesmentReservas.API.Data;
using AssesmentReservas.API.DTOs.Notifications;
using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Interfaces.Notifications;
using AssesmentReservas.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AssesmentReservas.API.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, IEmailSender email, ILogger<NotificationService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task NotifyAsync(string userId, NotificationType type, string title, string message,
        bool sendEmail = true, CancellationToken ct = default)
    {
        // 1. Canal in-app: siempre se persiste.
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message
        });
        await _db.SaveChangesAsync(ct);

        // 2. Canal correo: best-effort. Un fallo de SMTP no debe romper el flujo de negocio.
        if (!sendEmail)
            return;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user?.Email is null)
            return;

        try
        {
            var name = $"{user.FirstName} {user.LastName}".Trim();
            await _email.SendAsync(user.Email, name, title, BuildHtml(title, message), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el envío de correo de notificación a {UserId}", userId);
        }
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(string userId, bool unreadOnly = false, CancellationToken ct = default)
    {
        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(ct);
    }

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
        => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAsReadAsync(int id, string userId, CancellationToken ct = default)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);
        if (notification is null || notification.IsRead)
            return;

        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    private static string BuildHtml(string title, string message) => $"""
        <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto">
          <h2 style="color:#2c3e50">{title}</h2>
          <p style="font-size:15px;color:#444">{message}</p>
          <hr/>
          <small style="color:#999">Assesment Reservas — notificación automática</small>
        </div>
        """;
}
