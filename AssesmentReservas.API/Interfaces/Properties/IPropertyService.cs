using AssesmentReservas.API.Common;
using AssesmentReservas.API.DTOs.Properties;

namespace AssesmentReservas.API.Interfaces.Properties;

public interface IPropertyService
{
    // Público (anónimo)
    Task<PagedResult<PropertyListItemDto>> SearchAsync(PropertySearchDto filters, CancellationToken ct = default);
    Task<PropertyDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);

    // Propietario
    Task<PagedResult<PropertyListItemDto>> GetByOwnerAsync(string ownerId, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<PropertyDetailDto>> CreateAsync(string ownerId, PropertyCreateDto dto, CancellationToken ct = default);
    Task<ServiceResult<PropertyDetailDto>> UpdateAsync(int id, string ownerId, PropertyCreateDto dto, CancellationToken ct = default);
    Task<ServiceResult> DeactivateAsync(int id, string ownerId, CancellationToken ct = default);
    Task<ServiceResult<PropertyImageDto>> AddImageAsync(int id, string ownerId, Stream content,
        string fileName, string contentType, bool isCover, CancellationToken ct = default);
}
