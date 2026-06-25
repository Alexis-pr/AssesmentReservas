using System.Text.Json;
using AssesmentReservas.API.Common;
using AssesmentReservas.API.Data;
using AssesmentReservas.API.DTOs.Properties;
using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Interfaces.Infrastructure;
using AssesmentReservas.API.Interfaces.Properties;
using AssesmentReservas.API.Models;
using AssesmentReservas.API.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace AssesmentReservas.API.Services.Properties;

public class PropertyService : IPropertyService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IDistributedCache _cache;
    private readonly MinioSettings _minio;
    private readonly ILogger<PropertyService> _logger;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public PropertyService(
        AppDbContext db,
        IFileStorageService storage,
        IDistributedCache cache,
        IOptions<MinioSettings> minio,
        ILogger<PropertyService> logger)
    {
        _db = db;
        _storage = storage;
        _cache = cache;
        _minio = minio.Value;
        _logger = logger;
    }

    public async Task<PagedResult<PropertyListItemDto>> SearchAsync(PropertySearchDto f, CancellationToken ct = default)
    {
        var page = Math.Max(1, f.Page);
        var pageSize = Math.Clamp(f.PageSize, 1, 50);

        var query = _db.Properties.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(f.City))
            query = query.Where(p => EF.Functions.ILike(p.City, $"%{f.City}%"));

        if (f.MinGuests is > 0)
            query = query.Where(p => p.MaxGuests >= f.MinGuests);

        // Filtro de disponibilidad: excluir inmuebles con reservas que se solapen.
        if (f.CheckIn is { } ci && f.CheckOut is { } co && co > ci)
        {
            query = query.Where(p => !p.Bookings.Any(b =>
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
                b.CheckInDate < co && ci < b.CheckOutDate));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PropertyListItemDto
            {
                Id = p.Id,
                Title = p.Title,
                City = p.City,
                Country = p.Country,
                PricePerNight = p.PricePerNight,
                MaxGuests = p.MaxGuests,
                CoverImageUrl = p.Images
                    .OrderByDescending(i => i.IsCover)
                    .Select(i => i.ObjectKey)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        // Construir URLs públicas (no se hace en la proyección de EF).
        foreach (var item in items.Where(i => i.CoverImageUrl is not null))
            item.CoverImageUrl = _storage.BuildPublicUrl(_minio.PublicBucket, item.CoverImageUrl!);

        return new PagedResult<PropertyListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<PropertyDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = CacheKey(id);
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<PropertyDetailDto>(cached);

        var property = await _db.Properties.AsNoTracking()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (property is null)
            return null;

        var dto = ToDetailDto(property);
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), CacheOptions, ct);
        return dto;
    }

    public async Task<PagedResult<PropertyListItemDto>> GetByOwnerAsync(string ownerId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Properties.AsNoTracking().Where(p => p.OwnerId == ownerId);
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PropertyListItemDto
            {
                Id = p.Id,
                Title = p.Title,
                City = p.City,
                Country = p.Country,
                PricePerNight = p.PricePerNight,
                MaxGuests = p.MaxGuests,
                CoverImageUrl = p.Images.OrderByDescending(i => i.IsCover).Select(i => i.ObjectKey).FirstOrDefault()
            })
            .ToListAsync(ct);

        foreach (var item in items.Where(i => i.CoverImageUrl is not null))
            item.CoverImageUrl = _storage.BuildPublicUrl(_minio.PublicBucket, item.CoverImageUrl!);

        return new PagedResult<PropertyListItemDto> { Items = items, Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<ServiceResult<PropertyDetailDto>> CreateAsync(string ownerId, PropertyCreateDto dto, CancellationToken ct = default)
    {
        var property = new Property
        {
            OwnerId = ownerId,
            Title = dto.Title,
            Description = dto.Description,
            City = dto.City,
            Country = dto.Country,
            AddressLine = dto.AddressLine,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            PricePerNight = dto.PricePerNight,
            MaxGuests = dto.MaxGuests,
            Bedrooms = dto.Bedrooms,
            Bathrooms = dto.Bathrooms
        };

        _db.Properties.Add(property);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Inmueble {Id} creado por {OwnerId}", property.Id, ownerId);
        return ServiceResult<PropertyDetailDto>.Success(ToDetailDto(property));
    }

    public async Task<ServiceResult<PropertyDetailDto>> UpdateAsync(int id, string ownerId, PropertyCreateDto dto, CancellationToken ct = default)
    {
        var property = await _db.Properties.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (property is null)
            return ServiceResult<PropertyDetailDto>.Failure("Inmueble no encontrado.");
        if (property.OwnerId != ownerId)
            return ServiceResult<PropertyDetailDto>.Failure("No es propietario de este inmueble.");

        property.Title = dto.Title;
        property.Description = dto.Description;
        property.City = dto.City;
        property.Country = dto.Country;
        property.AddressLine = dto.AddressLine;
        property.Latitude = dto.Latitude;
        property.Longitude = dto.Longitude;
        property.PricePerNight = dto.PricePerNight;
        property.MaxGuests = dto.MaxGuests;
        property.Bedrooms = dto.Bedrooms;
        property.Bathrooms = dto.Bathrooms;
        property.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await InvalidateAsync(id, ct);

        return ServiceResult<PropertyDetailDto>.Success(ToDetailDto(property));
    }

    public async Task<ServiceResult> DeactivateAsync(int id, string ownerId, CancellationToken ct = default)
    {
        var property = await _db.Properties.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (property is null)
            return ServiceResult.Failure("Inmueble no encontrado.");
        if (property.OwnerId != ownerId)
            return ServiceResult.Failure("No es propietario de este inmueble.");

        property.IsActive = false;
        property.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await InvalidateAsync(id, ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<PropertyImageDto>> AddImageAsync(int id, string ownerId, Stream content,
        string fileName, string contentType, bool isCover, CancellationToken ct = default)
    {
        var property = await _db.Properties.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (property is null)
            return ServiceResult<PropertyImageDto>.Failure("Inmueble no encontrado.");
        if (property.OwnerId != ownerId)
            return ServiceResult<PropertyImageDto>.Failure("No es propietario de este inmueble.");

        var key = await _storage.UploadAsync(_minio.PublicBucket, content, fileName, contentType, publicRead: true, ct);

        if (isCover)
            foreach (var img in property.Images) img.IsCover = false;

        var image = new PropertyImage { PropertyId = id, ObjectKey = key, IsCover = isCover };
        _db.PropertyImages.Add(image);
        await _db.SaveChangesAsync(ct);
        await InvalidateAsync(id, ct);

        return ServiceResult<PropertyImageDto>.Success(new PropertyImageDto
        {
            Id = image.Id,
            Url = _storage.BuildPublicUrl(_minio.PublicBucket, key),
            IsCover = image.IsCover
        });
    }

    // ----- helpers -----

    private static string CacheKey(int id) => $"property:detail:{id}";

    private Task InvalidateAsync(int id, CancellationToken ct) => _cache.RemoveAsync(CacheKey(id), ct);

    private PropertyDetailDto ToDetailDto(Property p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Description = p.Description,
        City = p.City,
        Country = p.Country,
        AddressLine = p.AddressLine,
        Latitude = p.Latitude,
        Longitude = p.Longitude,
        PricePerNight = p.PricePerNight,
        MaxGuests = p.MaxGuests,
        Bedrooms = p.Bedrooms,
        Bathrooms = p.Bathrooms,
        IsActive = p.IsActive,
        OwnerId = p.OwnerId,
        Images = p.Images
            .OrderByDescending(i => i.IsCover)
            .Select(i => new PropertyImageDto
            {
                Id = i.Id,
                Url = _storage.BuildPublicUrl(_minio.PublicBucket, i.ObjectKey),
                IsCover = i.IsCover
            })
            .ToList()
    };
}
