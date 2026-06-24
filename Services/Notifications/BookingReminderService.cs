using AssesmentReservas.API.Data;
using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Interfaces.Notifications;
using Microsoft.EntityFrameworkCore;

namespace AssesmentReservas.API.Services.Notifications;

/// <summary>
/// Job en segundo plano que despacha recordatorios de check-in/check-out y marca
/// como completadas las estancias ya finalizadas. Idempotente: usa marcas de tiempo
/// en la reserva para no reenviar.
/// </summary>
public class BookingReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingReminderService> _logger;
    private readonly TimeSpan _period;
    private readonly int _daysBeforeCheckIn;

    public BookingReminderService(IServiceScopeFactory scopeFactory, IConfiguration config,
        ILogger<BookingReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _period = TimeSpan.FromMinutes(config.GetValue("Reminders:IntervalMinutes", 60));
        _daysBeforeCheckIn = config.GetValue("Reminders:DaysBeforeCheckIn", 1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);
        do
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el ciclo de recordatorios de reservas");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var checkInWindow = today.AddDays(_daysBeforeCheckIn);

        // 1. Recordatorios de llegada (dentro de la ventana previa).
        var arriving = await db.Bookings.Include(b => b.Property)
            .Where(b => b.Status == BookingStatus.Confirmed
                && b.CheckInReminderSentAt == null
                && b.CheckInDate >= today && b.CheckInDate <= checkInWindow)
            .ToListAsync(ct);

        foreach (var b in arriving)
        {
            await notifications.NotifyAsync(b.GuestId, NotificationType.CheckInReminder,
                "Recordatorio de llegada",
                $"Tu check-in en \"{b.Property!.Title}\" es el {b.CheckInDate:yyyy-MM-dd} a las 14:00.",
                ct: ct);
            b.CheckInReminderSentAt = DateTime.UtcNow;
        }

        // 2. Recordatorios de salida (el día del check-out).
        var leaving = await db.Bookings.Include(b => b.Property)
            .Where(b => b.Status == BookingStatus.Confirmed
                && b.CheckOutReminderSentAt == null
                && b.CheckOutDate == today)
            .ToListAsync(ct);

        foreach (var b in leaving)
        {
            await notifications.NotifyAsync(b.GuestId, NotificationType.CheckOutReminder,
                "Recordatorio de salida",
                $"Tu check-out en \"{b.Property!.Title}\" es hoy a las 12:00. ¡Buen viaje!",
                ct: ct);
            b.CheckOutReminderSentAt = DateTime.UtcNow;
        }

        // 3. Marcar como completadas las estancias finalizadas.
        var finished = await db.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed && b.CheckOutDate < today)
            .ToListAsync(ct);

        foreach (var b in finished)
            b.Status = BookingStatus.Completed;

        await db.SaveChangesAsync(ct);

        if (arriving.Count + leaving.Count + finished.Count > 0)
            _logger.LogInformation("Recordatorios: {In} llegada, {Out} salida, {Done} completadas",
                arriving.Count, leaving.Count, finished.Count);
    }
}
