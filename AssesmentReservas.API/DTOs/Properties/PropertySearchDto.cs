namespace AssesmentReservas.API.DTOs.Properties;

/// <summary>Filtros del catálogo público (todos opcionales). Búsqueda anónima.</summary>
public class PropertySearchDto
{
    public string? City { get; set; }
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int? MinGuests { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
