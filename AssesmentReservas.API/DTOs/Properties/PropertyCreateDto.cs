using System.ComponentModel.DataAnnotations;

namespace AssesmentReservas.API.DTOs.Properties;

/// <summary>Datos para publicar/editar un inmueble.</summary>
public class PropertyCreateDto
{
    [Required, MaxLength(150)]
    public string Title { get; set; } = default!;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = default!;

    [Required, MaxLength(120)]
    public string City { get; set; } = default!;

    [Required, MaxLength(120)]
    public string Country { get; set; } = default!;

    [MaxLength(250)]
    public string? AddressLine { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal PricePerNight { get; set; }

    [Range(1, 100)]
    public int MaxGuests { get; set; }

    [Range(0, 50)]
    public int Bedrooms { get; set; }

    [Range(0, 50)]
    public int Bathrooms { get; set; }
}
