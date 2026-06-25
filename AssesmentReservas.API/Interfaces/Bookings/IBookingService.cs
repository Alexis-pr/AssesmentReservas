using AssesmentReservas.API.Common;
using AssesmentReservas.API.DTOs.Bookings;

namespace AssesmentReservas.API.Interfaces.Bookings;

public interface IBookingService
{
    Task<ServiceResult<BookingDto>> CreateAsync(string guestId, BookingCreateDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<BookingDto>> GetMineAsync(string guestId, CancellationToken ct = default);
    Task<BookingDto?> GetByIdAsync(int id, string guestId, CancellationToken ct = default);
    Task<ServiceResult> CancelAsync(int id, string guestId, CancellationToken ct = default);
}
