using System.ComponentModel.DataAnnotations;

namespace AssesmentReservas.API.DTOs.Identity;

/// <summary>Datos para registrar un nuevo usuario.</summary>
public class RegisterDto
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = default!;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = default!;

    [Required, MaxLength(150)]
    public string FirstName { get; set; } = default!;

    [Required, MaxLength(150)]
    public string LastName { get; set; } = default!;

    /// <summary>Rol solicitado: "Guest" (por defecto) u "Owner".</summary>
    [MaxLength(20)]
    public string? Role { get; set; }
}
