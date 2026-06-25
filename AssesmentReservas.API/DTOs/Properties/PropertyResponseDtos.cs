namespace AssesmentReservas.API.DTOs.Properties;

/// <summary>Ítem resumido para el catálogo/listados.</summary>
public class PropertyListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string City { get; set; } = default!;
    public string Country { get; set; } = default!;
    public decimal PricePerNight { get; set; }
    public int MaxGuests { get; set; }
    public string? CoverImageUrl { get; set; }
}

/// <summary>Detalle completo de un inmueble.</summary>
public class PropertyDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string City { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string? AddressLine { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal PricePerNight { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public bool IsActive { get; set; }
    public string OwnerId { get; set; } = default!;
    public IReadOnlyList<PropertyImageDto> Images { get; set; } = [];
}

public class PropertyImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = default!;
    public bool IsCover { get; set; }
}
