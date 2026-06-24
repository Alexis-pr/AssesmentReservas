namespace AssesmentReservas.API.DTOs.Owner;

/// <summary>Panel de rendimiento consolidado del propietario para un periodo.</summary>
public class DashboardDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }

    public int PropertiesCount { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }

    /// <summary>Tasa de ocupación 0..1 (noches reservadas / noches disponibles).</summary>
    public double OccupancyRate { get; set; }

    public IReadOnlyList<PropertyMetricDto> Properties { get; set; } = [];
}

/// <summary>Métricas de un inmueble dentro del periodo.</summary>
public class PropertyMetricDto
{
    public int PropertyId { get; set; }
    public string Title { get; set; } = default!;
    public int Bookings { get; set; }
    public int NightsBooked { get; set; }
    public decimal Revenue { get; set; }
    public double OccupancyRate { get; set; }
}
