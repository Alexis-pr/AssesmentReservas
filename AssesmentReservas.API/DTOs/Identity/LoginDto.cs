using System.ComponentModel.DataAnnotations;

namespace AssesmentReservas.API.DTOs.Identity;

/// <summary>Credenciales para iniciar sesión.</summary>
public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;

    public bool RememberMe { get; set; } = true;
}
