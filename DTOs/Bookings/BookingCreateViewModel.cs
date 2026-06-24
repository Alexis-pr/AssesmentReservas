using AssesmentReservas.API.DTOs.Properties;

namespace AssesmentReservas.API.DTOs.Bookings;

/// <summary>Modelo para la pantalla de reserva (resumen del inmueble + estado KYC + formulario).</summary>
public class BookingCreateViewModel
{
    public PropertyDetailDto Property { get; set; } = default!;
    public bool IsKycVerified { get; set; }
    public BookingCreateDto Form { get; set; } = new();
}
