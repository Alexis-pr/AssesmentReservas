using AssesmentReservas.API.Interfaces.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers;

/// <summary>Centro de notificaciones in-app por UI (MVC).</summary>
[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    private string CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _notifications.GetForUserAsync(CurrentUserId, false, ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await _notifications.MarkAsReadAsync(id, CurrentUserId, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _notifications.MarkAllAsReadAsync(CurrentUserId, ct);
        return RedirectToAction(nameof(Index));
    }
}
