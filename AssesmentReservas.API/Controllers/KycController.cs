using AssesmentReservas.API.DTOs.Identity;
using AssesmentReservas.API.Interfaces.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers;

/// <summary>Validación de identidad por UI (MVC): subir cédula y ver veredicto.</summary>
[Authorize]
public class KycController : Controller
{
    private readonly IKycService _kyc;

    public KycController(IKycService kyc) => _kyc = kyc;

    private string CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var status = await _kyc.GetStatusAsync(CurrentUserId, ct);
        return View(new KycPageViewModel { Status = status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Selecciona una imagen de tu documento.");
            var status0 = await _kyc.GetStatusAsync(CurrentUserId, ct);
            return View("Index", new KycPageViewModel { Status = status0 });
        }

        await using var stream = file.OpenReadStream();
        var result = await _kyc.SubmitAsync(CurrentUserId, stream, file.FileName, file.ContentType, ct);

        var status = await _kyc.GetStatusAsync(CurrentUserId, ct);
        var vm = new KycPageViewModel { Status = status, Result = result.Data };

        if (!result.Succeeded)
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);

        return View("Index", vm);
    }
}
