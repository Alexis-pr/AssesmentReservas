using System.ComponentModel.DataAnnotations;

namespace AssesmentReservas.API.DTOs.Bookings;

/// <summary>Datos para crear una reserva. Las horas (14:00/12:00) las fija el sistema.</summary>
public class BookingCreateDto
{
    [Required]
    public int PropertyId { get; set; }

    [Required]
    public DateOnly CheckInDate { get; set; }

    [Required]
    public DateOnly CheckOutDate { get; set; }
}
