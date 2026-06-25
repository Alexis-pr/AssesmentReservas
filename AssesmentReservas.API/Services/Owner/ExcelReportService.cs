using AssesmentReservas.API.Data;
using AssesmentReservas.API.Interfaces.Owner;
using AssesmentReservas.API.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AssesmentReservas.API.Services.Owner;

public class ExcelReportService : IReportService
{
    private readonly AppDbContext _db;

    public ExcelReportService(AppDbContext db) => _db = db;

    public async Task<byte[]> GenerateBookingsExcelAsync(string ownerId, DateOnly from, DateOnly to,
        int? propertyId = null, CancellationToken ct = default)
    {
        var rangeEnd = to.AddDays(1);

        var rows = await _db.Bookings.AsNoTracking()
            .Where(b => b.Property!.OwnerId == ownerId
                && (propertyId == null || b.PropertyId == propertyId)
                && b.CheckInDate < rangeEnd && from < b.CheckOutDate)
            .OrderBy(b => b.CheckInDate)
            .Select(b => new
            {
                b.Id,
                Property = b.Property!.Title,
                b.Property.City,
                GuestName = (b.Guest!.FirstName ?? "") + " " + (b.Guest.LastName ?? ""),
                GuestEmail = b.Guest.Email,
                b.CheckInDate,
                b.CheckOutDate,
                Nights = b.CheckOutDate.DayNumber - b.CheckInDate.DayNumber,
                b.PricePerNight,
                b.TotalPrice,
                Status = b.Status.ToString()
            })
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Reservas");

        string[] headers =
        [
            "Reserva", "Inmueble", "Ciudad", "Huésped", "Correo",
            "Check-in (14:00)", "Check-out (12:00)", "Noches", "Precio/noche", "Total", "Estado"
        ];

        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2c3e50");
        headerRange.Style.Font.FontColor = XLColor.White;

        var r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.Id;
            ws.Cell(r, 2).Value = row.Property;
            ws.Cell(r, 3).Value = row.City;
            ws.Cell(r, 4).Value = row.GuestName.Trim();
            ws.Cell(r, 5).Value = row.GuestEmail;
            ws.Cell(r, 6).Value = row.CheckInDate.ToDateTime(new TimeOnly(14, 0));
            ws.Cell(r, 7).Value = row.CheckOutDate.ToDateTime(new TimeOnly(12, 0));
            ws.Cell(r, 8).Value = row.Nights;
            ws.Cell(r, 9).Value = row.PricePerNight;
            ws.Cell(r, 10).Value = row.TotalPrice;
            ws.Cell(r, 11).Value = row.Status;
            r++;
        }

        ws.Range(2, 6, Math.Max(2, r - 1), 7).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        ws.Range(2, 9, Math.Max(2, r - 1), 10).Style.NumberFormat.Format = "#,##0.00";

        // Fila de total general.
        if (rows.Count > 0)
        {
            ws.Cell(r, 9).Value = "TOTAL";
            ws.Cell(r, 9).Style.Font.Bold = true;
            ws.Cell(r, 10).FormulaA1 = $"SUM(J2:J{r - 1})";
            ws.Cell(r, 10).Style.Font.Bold = true;
            ws.Cell(r, 10).Style.NumberFormat.Format = "#,##0.00";
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
