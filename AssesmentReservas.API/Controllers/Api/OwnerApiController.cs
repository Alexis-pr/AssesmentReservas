using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Interfaces.Owner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentReservas.API.Controllers.Api;

[Route("api/owner")]
[Authorize(Roles = Roles.Owner)]
public class OwnerApiController : ApiControllerBase
{
    private readonly IDashboardService _dashboard;
    private readonly IReportService _reports;

    public OwnerApiController(IDashboardService dashboard, IReportService reports)
    {
        _dashboard = dashboard;
        _reports = reports;
    }

    /// <summary>Panel de rendimiento (ingresos, ocupación, reservas) por periodo seleccionable.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? propertyId, CancellationToken ct)
    {
        var (f, t) = ResolveRange(from, to);
        return Ok(await _dashboard.GetDashboardAsync(CurrentUserId, f, t, propertyId, ct));
    }

    /// <summary>Descarga el reporte de reservas en Excel (.xlsx), todo el portafolio o un inmueble.</summary>
    [HttpGet("reports/excel")]
    public async Task<IActionResult> ReportExcel(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? propertyId, CancellationToken ct)
    {
        var (f, t) = ResolveRange(from, to);
        var bytes = await _reports.GenerateBookingsExcelAsync(CurrentUserId, f, t, propertyId, ct);

        var fileName = $"reservas_{f:yyyyMMdd}_{t:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>Por defecto: últimos 30 días.</summary>
    private static (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var t = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? t.AddDays(-30);
        return (f, t);
    }
}
