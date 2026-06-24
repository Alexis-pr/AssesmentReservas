using AssesmentReservas.API.Enums;

namespace AssesmentReservas.API.Models;

/// <summary>
/// Reserva de un inmueble. Las fechas se guardan como DateOnly; las horas estándar
/// (Check-in 14:00 / Check-out 12:00) se aplican vía <see cref="BookingPolicy"/>.
/// </summary>
public class Booking
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    /// <summary>Huésped que reserva (FK a ApplicationUser).</summary>
    public string GuestId { get; set; } = default!;
    public ApplicationUser? Guest { get; set; }

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }

    /// <summary>Tarifa por noche congelada al momento de reservar.</summary>
    public decimal PricePerNight { get; set; }

    public int Nights => Math.Max(0, CheckOutDate.DayNumber - CheckInDate.DayNumber);
    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Control de recordatorios automáticos (evita reenvíos).
    public DateTime? CheckInReminderSentAt { get; set; }
    public DateTime? CheckOutReminderSentAt { get; set; }
}
