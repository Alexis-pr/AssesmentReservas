namespace AssesmentReservas.API.DTOs.Identity;

/// <summary>Modelo de la pantalla de KYC: estado actual + resultado del último intento (si aplica).</summary>
public class KycPageViewModel
{
    public KycStatusDto Status { get; set; } = new();
    public KycResultDto? Result { get; set; }
}
