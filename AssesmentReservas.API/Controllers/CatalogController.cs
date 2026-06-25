using AssesmentReservas.API.DTOs.Properties;
using AssesmentReservas.API.Interfaces.Bookings;
using AssesmentReservas.API.Interfaces.Properties;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers;

/// <summary>Catálogo público (navegación anónima): explorar y ver detalle.</summary>
public class CatalogController : Controller
{
    private readonly IPropertyService _properties;
    private readonly IFavoriteService _favorites;

    public CatalogController(IPropertyService properties, IFavoriteService favorites)
    {
        _properties = properties;
        _favorites = favorites;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] PropertySearchDto filter, CancellationToken ct)
    {
        var results = await _properties.SearchAsync(filter, ct);
        ViewBag.Filter = filter;
        return View(results);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var property = await _properties.GetByIdAsync(id, ct);
        if (property is null || !property.IsActive)
            return NotFound();

        // Marca si el inmueble ya está en la wishlist del usuario (si está logueado).
        ViewBag.IsFavorite = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            var favs = await _favorites.GetForUserAsync(userId, ct);
            ViewBag.IsFavorite = favs.Any(f => f.Id == id);
        }

        return View(property);
    }
}
