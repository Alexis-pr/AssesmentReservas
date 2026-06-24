using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers.Api;

/// <summary>Base para controladores de API con utilidades de identidad.</summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id del usuario autenticado (claim NameIdentifier).</summary>
    protected string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Usuario no autenticado.");
}
