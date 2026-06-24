namespace AssesmentReservas.API.DTOs.Identity;

/// <summary>Resultado del proceso de validación de identidad.</summary>
public class KycResultDto
{
    public string Status { get; set; } = default!;
    public bool IsKycVerified { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>Estado actual del KYC del usuario.</summary>
public class KycStatusDto
{
    public string Status { get; set; } = "NotStarted";
    public bool IsKycVerified { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
