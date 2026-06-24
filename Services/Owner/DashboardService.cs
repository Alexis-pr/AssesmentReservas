using AssesmentReservas.API.Data;
using AssesmentReservas.API.DTOs.Owner;
using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Interfaces.Owner;
using Microsoft.EntityFrameworkCore;

namespace AssesmentReservas.API.Services.Owner;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    // Reservas que generan ingreso real (no canceladas).
    private static readonly BookingStatus[] RealizedStatuses =
        [BookingStatus.Confirmed, BookingStatus.Completed];

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardDto> GetDashboardAsync(string ownerId, DateOnly from, DateOnly to,
        int? propertyId = null, CancellationToken ct = default)
    {
        var rangeEnd = to.AddDays(1);                       // exclusivo
        var rangeNights = Math.Max(0, rangeEnd.DayNumber - from.DayNumber);

        var properties = await _db.Properties.AsNoTracking()
            .Where(p => p.OwnerId == ownerId && (propertyId == null || p.Id == propertyId))
            .Select(p => new { p.Id, p.Title })
            .ToListAsync(ct);

        var propIds = properties.Select(p => p.Id).ToList();

        var bookings = await _db.Bookings.AsNoTracking()
            .Where(b => propIds.Contains(b.PropertyId)
                && RealizedStatuses.Contains(b.Status)
                && b.CheckInDate < rangeEnd && from < b.CheckOutDate)
            .Select(b => new { b.PropertyId, b.CheckInDate, b.CheckOutDate, b.TotalPrice })
            .ToListAsync(ct);

        var perProperty = properties.Select(p =>
        {
            var own = bookings.Where(b => b.PropertyId == p.Id).ToList();
            var nights = own.Sum(b => OverlapNights(b.CheckInDate, b.CheckOutDate, from, rangeEnd));
            var revenue = own.Sum(b => b.TotalPrice);

            return new PropertyMetricDto
            {
                PropertyId = p.Id,
                Title = p.Title,
                Bookings = own.Count,
                NightsBooked = nights,
                Revenue = revenue,
                OccupancyRate = rangeNights == 0 ? 0 : Math.Round(nights / (double)rangeNights, 4)
            };
        }).ToList();

        var totalAvailableNights = properties.Count * rangeNights;
        var totalBookedNights = perProperty.Sum(p => p.NightsBooked);

        return new DashboardDto
        {
            From = from,
            To = to,
            PropertiesCount = properties.Count,
            TotalBookings = bookings.Count,
            TotalRevenue = bookings.Sum(b => b.TotalPrice),
            OccupancyRate = totalAvailableNights == 0 ? 0 : Math.Round(totalBookedNights / (double)totalAvailableNights, 4),
            Properties = perProperty
        };
    }

    /// <summary>Noches de una reserva que caen dentro del rango [from, rangeEnd).</summary>
    private static int OverlapNights(DateOnly checkIn, DateOnly checkOut, DateOnly from, DateOnly rangeEnd)
    {
        var start = checkIn > from ? checkIn : from;
        var end = checkOut < rangeEnd ? checkOut : rangeEnd;
        return Math.Max(0, end.DayNumber - start.DayNumber);
    }
}
