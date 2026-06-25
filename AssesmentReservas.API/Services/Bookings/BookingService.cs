using System.Data;
using AssesmentReservas.API.Common;
using AssesmentReservas.API.Data;
using AssesmentReservas.API.DTOs.Bookings;
using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Interfaces.Bookings;
using AssesmentReservas.API.Interfaces.Notifications;
using AssesmentReservas.API.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AssesmentReservas.API.Services.Bookings;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<BookingService> _logger;

    // Estados que ocupan el calendario del inmueble.
    private static readonly BookingStatus[] BlockingStatuses =
        [BookingStatus.Pending, BookingStatus.Confirmed];

    public BookingService(AppDbContext db, INotificationService notifications, ILogger<BookingService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ServiceResult<BookingDto>> CreateAsync(string guestId, BookingCreateDto dto, CancellationToken ct = default)
    {
        // 1. Validación de fechas.
        if (dto.CheckOutDate <= dto.CheckInDate)
            return ServiceResult<BookingDto>.Failure("La fecha de salida debe ser posterior a la de llegada.");
        if (dto.CheckInDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return ServiceResult<BookingDto>.Failure("La fecha de llegada no puede estar en el pasado.");

        // 2. Gate KYC: no se permite la (primera) reserva sin validación de identidad aprobada.
        var guest = await _db.Users.FirstOrDefaultAsync(u => u.Id == guestId, ct);
        if (guest is null)
            return ServiceResult<BookingDto>.Failure("Usuario no encontrado.");
        if (!guest.IsKycVerified)
            return ServiceResult<BookingDto>.Failure("Debes completar la validación de identidad (KYC) antes de reservar.");

        // 3. Inmueble válido y activo.
        var property = await _db.Properties.FirstOrDefaultAsync(p => p.Id == dto.PropertyId && p.IsActive, ct);
        if (property is null)
            return ServiceResult<BookingDto>.Failure("Inmueble no encontrado o no disponible.");

        var nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;

        // 4. Anti double-booking: re-chequeo de solapamiento dentro de una transacción
        //    serializable para evitar condiciones de carrera entre dos reservas simultáneas.
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var overlaps = await _db.Bookings.AnyAsync(b =>
                b.PropertyId == dto.PropertyId &&
                BlockingStatuses.Contains(b.Status) &&
                b.CheckInDate < dto.CheckOutDate &&
                dto.CheckInDate < b.CheckOutDate, ct);

            if (overlaps)
                return ServiceResult<BookingDto>.Failure("Las fechas seleccionadas ya no están disponibles.");

            var booking = new Booking
            {
                PropertyId = property.Id,
                GuestId = guestId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                PricePerNight = property.PricePerNight,
                TotalPrice = property.PricePerNight * nights,
                Status = BookingStatus.Confirmed,
                ConfirmedAt = DateTime.UtcNow
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation("Reserva {Id} confirmada: inmueble {PropertyId}, {CheckIn}->{CheckOut}",
                booking.Id, property.Id, dto.CheckInDate, dto.CheckOutDate);

            await _notifications.NotifyAsync(guestId, NotificationType.BookingConfirmed,
                "Reserva confirmada",
                $"Tu reserva en \"{property.Title}\" del {dto.CheckInDate:yyyy-MM-dd} (14:00) " +
                $"al {dto.CheckOutDate:yyyy-MM-dd} (12:00) quedó confirmada. Total: {booking.TotalPrice.Cop()}.",
                ct: ct);

            return ServiceResult<BookingDto>.Success(ToDto(booking, property.Title));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "40001" })
        {
            // Conflicto de serialización: otra reserva tomó las fechas al mismo tiempo.
            await tx.RollbackAsync(ct);
            return ServiceResult<BookingDto>.Failure("Las fechas seleccionadas ya no están disponibles.");
        }
    }

    public async Task<IReadOnlyList<BookingDto>> GetMineAsync(string guestId, CancellationToken ct = default)
    {
        var bookings = await _db.Bookings.AsNoTracking()
            .Include(b => b.Property)
            .Where(b => b.GuestId == guestId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        return bookings.Select(b => ToDto(b, b.Property!.Title)).ToList();
    }

    public async Task<BookingDto?> GetByIdAsync(int id, string guestId, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.AsNoTracking()
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == guestId, ct);

        return booking is null ? null : ToDto(booking, booking.Property!.Title);
    }

    public async Task<ServiceResult> CancelAsync(int id, string guestId, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.GuestId == guestId, ct);
        if (booking is null)
            return ServiceResult.Failure("Reserva no encontrada.");
        if (booking.Status == BookingStatus.Cancelled)
            return ServiceResult.Success();
        if (booking.Status == BookingStatus.Completed)
            return ServiceResult.Failure("No se puede cancelar una estancia ya completada.");

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Reserva {Id} cancelada por {GuestId}", id, guestId);

        await _notifications.NotifyAsync(guestId, NotificationType.BookingCancelled,
            "Reserva cancelada",
            $"Tu reserva #{booking.Id} fue cancelada. Las fechas quedaron liberadas.",
            ct: ct);

        return ServiceResult.Success();
    }

    private static BookingDto ToDto(Booking b, string propertyTitle) => new()
    {
        Id = b.Id,
        PropertyId = b.PropertyId,
        PropertyTitle = propertyTitle,
        CheckInDate = b.CheckInDate,
        CheckOutDate = b.CheckOutDate,
        CheckInDateTime = BookingPolicy.CheckInAt(b.CheckInDate),
        CheckOutDateTime = BookingPolicy.CheckOutAt(b.CheckOutDate),
        Nights = b.CheckOutDate.DayNumber - b.CheckInDate.DayNumber,
        PricePerNight = b.PricePerNight,
        TotalPrice = b.TotalPrice,
        Status = b.Status.ToString(),
        CreatedAt = b.CreatedAt
    };
}
