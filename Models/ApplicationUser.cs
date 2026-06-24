using Microsoft.AspNetCore.Identity;

namespace AssesmentReservas.API.Models;

/// <summary>
/// Usuario de la plataforma. Extiende IdentityUser para soportar autenticación por
/// cookies + Identity. Un mismo usuario puede actuar como huésped y/o propietario.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>True una vez que el KYC fue aprobado. Bloquea la primera reserva si es false.</summary>
    public bool IsKycVerified { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegación
    public ICollection<Property> Properties { get; set; } = new List<Property>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
