namespace AssesmentReservas.API.DTOs.Bookings;

/// <summary>Vista de una reserva, incluyendo las políticas de horario estándar.</summary>
public class BookingDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string PropertyTitle { get; set; } = default!;

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }

    /// <summary>Check-in con hora estándar (14:00).</summary>
    public DateTime CheckInDateTime { get; set; }

    /// <summary>Check-out con hora estándar (12:00).</summary>
    public DateTime CheckOutDateTime { get; set; }

    public int Nights { get; set; }
    public decimal PricePerNight { get; set; }
    public decimal TotalPrice { get; set; }

    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
