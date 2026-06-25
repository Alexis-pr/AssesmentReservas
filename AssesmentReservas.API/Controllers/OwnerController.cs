using AssesmentReservas.API.DTOs.Properties;
using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Interfaces.Owner;
using AssesmentReservas.API.Interfaces.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers;

/// <summary>Panel del propietario por UI (MVC): dashboard, inventario y reportes.</summary>
[Authorize(Roles = Roles.Owner)]
public class OwnerController : Controller
{
    private readonly IDashboardService _dashboard;
    private readonly IReportService _reports;
    private readonly IPropertyService _properties;

    public OwnerController(IDashboardService dashboard, IReportService reports, IPropertyService properties)
    {
        _dashboard = dashboard;
        _reports = reports;
        _properties = properties;
    }

    private string CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    private static (DateOnly From, DateOnly To) Range(DateOnly? from, DateOnly? to)
    {
        var t = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? t.AddDays(-30);
        return (f, t);
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var (f, t) = Range(from, to);
        return View(await _dashboard.GetDashboardAsync(CurrentUserId, f, t, null, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Properties(CancellationToken ct)
        => View(await _properties.GetByOwnerAsync(CurrentUserId, 1, 50, ct));

    [HttpGet]
    public IActionResult Create() => View(new PropertyCreateDto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _properties.CreateAsync(CurrentUserId, dto, ct);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            return View(dto);
        }
        TempData["Success"] = "Inmueble publicado.";
        return RedirectToAction(nameof(Manage), new { id = result.Data!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Manage(int id, CancellationToken ct)
    {
        var property = await _properties.GetByIdAsync(id, ct);
        if (property is null || property.OwnerId != CurrentUserId) return NotFound();
        return View(property);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadImage(int id, IFormFile? file, bool isCover, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            TempData["Error"] = "Selecciona una imagen.";
        else
        {
            await using var stream = file.OpenReadStream();
            var result = await _properties.AddImageAsync(id, CurrentUserId, stream, file.FileName, file.ContentType, isCover, ct);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Imagen subida." : string.Join(" ", result.Errors);
        }
        return RedirectToAction(nameof(Manage), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var result = await _properties.DeactivateAsync(id, CurrentUserId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Inmueble desactivado." : string.Join(" ", result.Errors);
        return RedirectToAction(nameof(Properties));
    }

    [HttpGet]
    public async Task<IActionResult> ReportExcel(DateOnly? from, DateOnly? to, int? propertyId, CancellationToken ct)
    {
        var (f, t) = Range(from, to);
        var bytes = await _reports.GenerateBookingsExcelAsync(CurrentUserId, f, t, propertyId, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"reservas_{f:yyyyMMdd}_{t:yyyyMMdd}.xlsx");
    }
}
