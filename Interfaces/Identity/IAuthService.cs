using System.Security.Claims;
using AssesmentReservas.API.Common;
using AssesmentReservas.API.DTOs.Identity;

namespace AssesmentReservas.API.Interfaces.Identity;

/// <summary>Operaciones de registro y autenticación por cookies + Identity.</summary>
public interface IAuthService
{
    Task<ServiceResult<AuthUserDto>> RegisterAsync(RegisterDto dto);
    Task<ServiceResult<AuthUserDto>> LoginAsync(LoginDto dto);
    Task LogoutAsync();
    Task<AuthUserDto?> GetCurrentUserAsync(ClaimsPrincipal principal);
}
