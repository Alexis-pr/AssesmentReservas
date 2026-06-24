using AssesmentReservas.API.DTOs.Bookings;
using AssesmentReservas.API.Interfaces.Bookings;
using AssesmentReservas.API.Interfaces.Identity;
using AssesmentReservas.API.Interfaces.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers;

/// <summary>Flujo de reservas por UI (MVC): crear, listar y cancelar.</summary>
[Authorize]
public class BookingsController : Controller
{
    private readonly IBookingService _bookings;
    private readonly IPropertyService _properties;
    private readonly IAuthService _auth;

    public BookingsController(IBookingService bookings, IPropertyService properties, IAuthService auth)
    {
        _bookings = bookings;
        _properties = properties;
        _auth = auth;
    }

    private string CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var bookings = await _bookings.GetMineAsync(CurrentUserId, ct);
        return View(bookings);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int propertyId, CancellationToken ct)
    {
        var property = await _properties.GetByIdAsync(propertyId, ct);
        if (property is null || !property.IsActive)
            return NotFound();

        var user = await _auth.GetCurrentUserAsync(User);
        return View(new BookingCreateViewModel
        {
            Property = property,
            IsKycVerified = user?.IsKycVerified ?? false,
            Form = new BookingCreateDto { PropertyId = propertyId }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateDto form, CancellationToken ct)
    {
        var result = await _bookings.CreateAsync(CurrentUserId, form, ct);
        if (result.Succeeded)
        {
            TempData["Success"] = "¡Reserva confirmada! Te enviamos la confirmación por correo.";
            return RedirectToAction(nameof(Index));
        }

        // Re-renderizar con el resumen del inmueble y los errores.
        var property = await _properties.GetByIdAsync(form.PropertyId, ct);
        if (property is null) return NotFound();

        var user = await _auth.GetCurrentUserAsync(User);
        foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);

        return View(new BookingCreateViewModel
        {
            Property = property,
            IsKycVerified = user?.IsKycVerified ?? false,
            Form = form
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await _bookings.CancelAsync(id, CurrentUserId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Reserva cancelada." : string.Join(" ", result.Errors);
        return RedirectToAction(nameof(Index));
    }
}
