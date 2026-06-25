using AssesmentReservas.API.DTOs.Bookings;
using AssesmentReservas.API.Interfaces.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers.Api;

[Route("api/bookings")]
[Authorize] // Reservar requiere sesión.
public class BookingsApiController : ApiControllerBase
{
    private readonly IBookingService _bookings;

    public BookingsApiController(IBookingService bookings) => _bookings = bookings;

    /// <summary>Crea una reserva (valida disponibilidad, horarios y KYC).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookingCreateDto dto, CancellationToken ct)
    {
        var result = await _bookings.CreateAsync(CurrentUserId, dto, ct);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>Mis reservas con sus políticas de horario.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
        => Ok(await _bookings.GetMineAsync(CurrentUserId, ct));

    /// <summary>Detalle de una reserva propia.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var booking = await _bookings.GetByIdAsync(id, CurrentUserId, ct);
        return booking is null ? NotFound() : Ok(booking);
    }

    /// <summary>Cancela una reserva propia (libera las fechas).</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await _bookings.CancelAsync(id, CurrentUserId, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
