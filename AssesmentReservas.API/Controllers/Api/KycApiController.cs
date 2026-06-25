using AssesmentReservas.API.Interfaces.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers.Api;

[Route("api/kyc")]
[Authorize]
public class KycApiController : ApiControllerBase
{
    private readonly IKycService _kyc;

    public KycApiController(IKycService kyc) => _kyc = kyc;

    /// <summary>Sube la foto de la cédula; corre OCR y emite veredicto (aprobado/rechazado).</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "Archivo vacío." } });

        await using var stream = file.OpenReadStream();
        var result = await _kyc.SubmitAsync(CurrentUserId, stream, file.FileName, file.ContentType, ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    /// <summary>Estado de validación de identidad del usuario.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
        => Ok(await _kyc.GetStatusAsync(CurrentUserId, ct));
}
