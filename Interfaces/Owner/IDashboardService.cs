using AssesmentReservas.API.DTOs.Owner;

namespace AssesmentReservas.API.Interfaces.Owner;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(string ownerId, DateOnly from, DateOnly to,
        int? propertyId = null, CancellationToken ct = default);
}
