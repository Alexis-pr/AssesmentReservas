using AssesmentReservas.API.Interfaces.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers.Api;

[Route("api/favorites")]
[Authorize] // Favoritos permanentes requieren sesión (cualquier rol).
public class FavoritesApiController : ApiControllerBase
{
    private readonly IFavoriteService _favorites;

    public FavoritesApiController(IFavoriteService favorites) => _favorites = favorites;

    /// <summary>Wishlist del usuario autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct)
        => Ok(await _favorites.GetForUserAsync(CurrentUserId, ct));

    /// <summary>Marca un inmueble como favorito (idempotente).</summary>
    [HttpPost("{propertyId:int}")]
    public async Task<IActionResult> Add(int propertyId, CancellationToken ct)
    {
        var result = await _favorites.AddAsync(CurrentUserId, propertyId, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    /// <summary>Quita un inmueble de la wishlist (idempotente).</summary>
    [HttpDelete("{propertyId:int}")]
    public async Task<IActionResult> Remove(int propertyId, CancellationToken ct)
    {
        var result = await _favorites.RemoveAsync(CurrentUserId, propertyId, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
