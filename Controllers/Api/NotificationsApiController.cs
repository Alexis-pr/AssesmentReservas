using AssesmentReservas.API.Interfaces.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers.Api;

[Route("api/notifications")]
[Authorize]
public class NotificationsApiController : ApiControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsApiController(INotificationService notifications) => _notifications = notifications;

    /// <summary>Notificaciones in-app del usuario (opcional: solo no leídas).</summary>
    [HttpGet]
    public async Task<IActionResult> Mine([FromQuery] bool unreadOnly = false, CancellationToken ct = default)
        => Ok(await _notifications.GetForUserAsync(CurrentUserId, unreadOnly, ct));

    /// <summary>Conteo de no leídas (para badge).</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
        => Ok(new { count = await _notifications.GetUnreadCountAsync(CurrentUserId, ct) });

    /// <summary>Marca una notificación como leída.</summary>
    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await _notifications.MarkAsReadAsync(id, CurrentUserId, ct);
        return NoContent();
    }

    /// <summary>Marca todas como leídas.</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _notifications.MarkAllAsReadAsync(CurrentUserId, ct);
        return NoContent();
    }
}
