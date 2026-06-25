using AssesmentReservas.API.Interfaces.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers;

/// <summary>Wishlist por UI (MVC).</summary>
[Authorize]
public class FavoritesController : Controller
{
    private readonly IFavoriteService _favorites;

    public FavoritesController(IFavoriteService favorites) => _favorites = favorites;

    private string CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _favorites.GetForUserAsync(CurrentUserId, ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int propertyId, string? returnUrl, CancellationToken ct)
    {
        await _favorites.AddAsync(CurrentUserId, propertyId, ct);
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int propertyId, string? returnUrl, CancellationToken ct)
    {
        await _favorites.RemoveAsync(CurrentUserId, propertyId, ct);
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!);
    }
}
