using AssesmentReservas.API.Common;
using AssesmentReservas.API.DTOs.Properties;

namespace AssesmentReservas.API.Interfaces.Properties;

/// <summary>Wishlist: marcar/desmarcar inmuebles y listarlos.</summary>
public interface IFavoriteService
{
    Task<IReadOnlyList<PropertyListItemDto>> GetForUserAsync(string userId, CancellationToken ct = default);
    Task<ServiceResult> AddAsync(string userId, int propertyId, CancellationToken ct = default);
    Task<ServiceResult> RemoveAsync(string userId, int propertyId, CancellationToken ct = default);
}
