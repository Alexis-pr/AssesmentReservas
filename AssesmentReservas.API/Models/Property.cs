using System.ComponentModel.DataAnnotations;

namespace AssesmentReservas.API.Models;

/// <summary>Inmueble publicado por un propietario para renta corta.</summary>
public class Property
{
    public int Id { get; set; }

    /// <summary>Propietario (FK a ApplicationUser).</summary>
    public string OwnerId { get; set; } = default!;
    public ApplicationUser? Owner { get; set; }

    [MaxLength(150)]
    public string Title { get; set; } = default!;

    [MaxLength(4000)]
    public string Description { get; set; } = default!;

    // Ubicación geográfica (para filtros de búsqueda).
    [MaxLength(120)]
    public string City { get; set; } = default!;

    [MaxLength(120)]
    public string Country { get; set; } = default!;

    [MaxLength(250)]
    public string? AddressLine { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Tarifa por noche.</summary>
    public decimal PricePerNight { get; set; }

    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navegación
    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
