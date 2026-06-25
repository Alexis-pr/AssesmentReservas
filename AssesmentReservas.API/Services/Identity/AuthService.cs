using System.Security.Claims;
using AssesmentReservas.API.Common;
using AssesmentReservas.API.DTOs.Identity;
using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Interfaces.Identity;
using AssesmentReservas.API.Models;
using Microsoft.AspNetCore.Identity;

namespace AssesmentReservas.API.Services.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthUserDto>> RegisterAsync(RegisterDto dto)
    {
        // Solo se permiten los roles del sistema; por defecto, Huésped.
        var role = NormalizeRole(dto.Role);
        if (role is null)
            return ServiceResult<AuthUserDto>.Failure($"Rol inválido. Use '{Roles.Guest}' u '{Roles.Owner}'.");

        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
            return ServiceResult<AuthUserDto>.Failure("Ya existe un usuario con ese correo.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };

        var created = await _userManager.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
            return ServiceResult<AuthUserDto>.Failure(created.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, role);
        await _signInManager.SignInAsync(user, isPersistent: true);

        _logger.LogInformation("Usuario {Email} registrado con rol {Role}", user.Email, role);
        return ServiceResult<AuthUserDto>.Success(await BuildDtoAsync(user));
    }

    public async Task<ServiceResult<AuthUserDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return ServiceResult<AuthUserDto>.Failure("Credenciales inválidas.");

        var result = await _signInManager.PasswordSignInAsync(
            user, dto.Password, isPersistent: dto.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return ServiceResult<AuthUserDto>.Failure("Cuenta bloqueada temporalmente por intentos fallidos.");
        if (!result.Succeeded)
            return ServiceResult<AuthUserDto>.Failure("Credenciales inválidas.");

        _logger.LogInformation("Usuario {Email} inició sesión", user.Email);
        return ServiceResult<AuthUserDto>.Success(await BuildDtoAsync(user));
    }

    public Task LogoutAsync() => _signInManager.SignOutAsync();

    public async Task<AuthUserDto?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);
        return user is null ? null : await BuildDtoAsync(user);
    }

    private async Task<AuthUserDto> BuildDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new AuthUserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsKycVerified = user.IsKycVerified,
            Roles = roles.ToList()
        };
    }

    private static string? NormalizeRole(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return Roles.Guest;

        return Roles.All.FirstOrDefault(r => r.Equals(requested, StringComparison.OrdinalIgnoreCase));
    }
}
