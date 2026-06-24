using AssesmentReservas.API.Common;
using AssesmentReservas.API.Data;
using AssesmentReservas.API.DTOs.Properties;
using AssesmentReservas.API.Interfaces.Infrastructure;
using AssesmentReservas.API.Interfaces.Properties;
using AssesmentReservas.API.Models;
using AssesmentReservas.API.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssesmentReservas.API.Services.Properties;

public class FavoriteService : IFavoriteService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly MinioSettings _minio;
    private readonly ILogger<FavoriteService> _logger;

    public FavoriteService(AppDbContext db, IFileStorageService storage,
        IOptions<MinioSettings> minio, ILogger<FavoriteService> logger)
    {
        _db = db;
        _storage = storage;
        _minio = minio.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PropertyListItemDto>> GetForUserAsync(string userId, CancellationToken ct = default)
    {
        var items = await _db.Favorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new PropertyListItemDto
            {
                Id = f.Property!.Id,
                Title = f.Property.Title,
                City = f.Property.City,
                Country = f.Property.Country,
                PricePerNight = f.Property.PricePerNight,
                MaxGuests = f.Property.MaxGuests,
                CoverImageUrl = f.Property.Images
                    .OrderByDescending(i => i.IsCover)
                    .Select(i => i.ObjectKey)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        foreach (var item in items.Where(i => i.CoverImageUrl is not null))
            item.CoverImageUrl = _storage.BuildPublicUrl(_minio.PublicBucket, item.CoverImageUrl!);

        return items;
    }

    public async Task<ServiceResult> AddAsync(string userId, int propertyId, CancellationToken ct = default)
    {
        var exists = await _db.Properties.AnyAsync(p => p.Id == propertyId && p.IsActive, ct);
        if (!exists)
            return ServiceResult.Failure("Inmueble no encontrado.");

        var already = await _db.Favorites.AnyAsync(f => f.UserId == userId && f.PropertyId == propertyId, ct);
        if (already)
            return ServiceResult.Success(); // idempotente

        _db.Favorites.Add(new Favorite { UserId = userId, PropertyId = propertyId });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Usuario {UserId} agregó inmueble {PropertyId} a favoritos", userId, propertyId);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveAsync(string userId, int propertyId, CancellationToken ct = default)
    {
        var favorite = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == propertyId, ct);

        if (favorite is null)
            return ServiceResult.Success(); // idempotente

        _db.Favorites.Remove(favorite);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}
