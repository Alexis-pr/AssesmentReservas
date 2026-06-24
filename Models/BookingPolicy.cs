namespace AssesmentReservas.API.Models;

/// <summary>
/// Política de horarios estándar. Toda reserva confirmada usa Check-in 14:00 y
/// Check-out 12:00. Centralizado aquí para no repetir literales por el código.
/// </summary>
public static class BookingPolicy
{
    public static readonly TimeOnly CheckInTime = new(14, 0);   // 2:00 PM
    public static readonly TimeOnly CheckOutTime = new(12, 0);  // 12:00 PM (mediodía)

    public static DateTime CheckInAt(DateOnly date) => date.ToDateTime(CheckInTime);
    public static DateTime CheckOutAt(DateOnly date) => date.ToDateTime(CheckOutTime);
}
