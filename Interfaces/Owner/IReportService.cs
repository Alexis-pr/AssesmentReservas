namespace AssesmentReservas.API.Interfaces.Owner;

public interface IReportService
{
    /// <summary>Genera un .xlsx con las reservas del propietario (todo el portafolio o un inmueble).</summary>
    Task<byte[]> GenerateBookingsExcelAsync(string ownerId, DateOnly from, DateOnly to,
        int? propertyId = null, CancellationToken ct = default);
}
