using AssesmentReservas.API.DTOs.Properties;
using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Interfaces.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers.Api;

[Route("api/properties")]
public class PropertiesApiController : ApiControllerBase
{
    private readonly IPropertyService _properties;

    public PropertiesApiController(IPropertyService properties) => _properties = properties;

    /// <summary>Catálogo público con filtros de ubicación y disponibilidad (anónimo).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] PropertySearchDto filters, CancellationToken ct)
        => Ok(await _properties.SearchAsync(filters, ct));

    /// <summary>Detalle de un inmueble (anónimo).</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var property = await _properties.GetByIdAsync(id, ct);
        return property is null ? NotFound() : Ok(property);
    }

    /// <summary>Inmuebles del propietario autenticado.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Owner)]
    public async Task<IActionResult> Mine([FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken ct = default)
        => Ok(await _properties.GetByOwnerAsync(CurrentUserId, page, pageSize, ct));

    /// <summary>Publica un nuevo inmueble.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Owner)]
    public async Task<IActionResult> Create([FromBody] PropertyCreateDto dto, CancellationToken ct)
    {
        var result = await _properties.CreateAsync(CurrentUserId, dto, ct);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>Edita un inmueble propio.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Owner)]
    public async Task<IActionResult> Update(int id, [FromBody] PropertyCreateDto dto, CancellationToken ct)
    {
        var result = await _properties.UpdateAsync(id, CurrentUserId, dto, ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    /// <summary>Desactiva (oculta) un inmueble propio.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Owner)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var result = await _properties.DeactivateAsync(id, CurrentUserId, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    /// <summary>Sube una imagen al inmueble (almacenada en MinIO).</summary>
    [HttpPost("{id:int}/images")]
    [Authorize(Roles = Roles.Owner)]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, [FromForm] bool isCover = false, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "Archivo vacío." } });

        await using var stream = file.OpenReadStream();
        var result = await _properties.AddImageAsync(id, CurrentUserId, stream, file.FileName, file.ContentType, isCover, ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }
}
