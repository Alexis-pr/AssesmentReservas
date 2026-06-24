namespace AssesmentReservas.API.Models;

/// <summary>Inmueble guardado en la wishlist de un usuario.</summary>
public class Favorite
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;
    public ApplicationUser? User { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
