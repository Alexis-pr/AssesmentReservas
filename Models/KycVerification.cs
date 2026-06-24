using System.ComponentModel.DataAnnotations;
using AssesmentReservas.API.Enums;

namespace AssesmentReservas.API.Models;

/// <summary>
/// Registro de validación de identidad. El documento se procesa con OCR (Tesseract),
/// se extraen los campos y se emite un veredicto. La imagen se guarda cifrada en MinIO
/// y se elimina de forma segura tras el procesamiento (privacidad de datos).
/// </summary>
public class KycVerification
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;
    public ApplicationUser? User { get; set; }

    // Campos extraídos por la IA/OCR.
    [MaxLength(150)]
    public string? FirstName { get; set; }

    [MaxLength(150)]
    public string? LastName { get; set; }

    [MaxLength(50)]
    public string? DocumentNumber { get; set; }

    public DateOnly? BirthDate { get; set; }

    public KycStatus Status { get; set; } = KycStatus.Pending;

    /// <summary>Object key del documento cifrado en MinIO (null tras eliminación segura).</summary>
    [MaxLength(300)]
    public string? DocumentObjectKey { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
