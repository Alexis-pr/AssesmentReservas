namespace AssesmentReservas.API.DTOs.Identity;

/// <summary>Representación pública del usuario autenticado.</summary>
public class AuthUserDto
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsKycVerified { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}
