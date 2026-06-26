using AssesmentReservas.API.Common;
using AssesmentReservas.API.Data;
using AssesmentReservas.API.DTOs.Identity;
using AssesmentReservas.API.Enums;
using AssesmentReservas.API.Interfaces.Identity;
using AssesmentReservas.API.Interfaces.Infrastructure;
using AssesmentReservas.API.Interfaces.Notifications;
using AssesmentReservas.API.Models;
using AssesmentReservas.API.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssesmentReservas.API.Services.Identity;

public class KycService : IKycService
{
    private readonly AppDbContext _db;
    private readonly IOcrService _ocr;
    private readonly IEncryptionService _encryption;
    private readonly IFileStorageService _storage;
    private readonly INotificationService _notifications;
    private readonly KycSettings _settings;
    private readonly MinioSettings _minio;
    private readonly ILogger<KycService> _logger;

    public KycService(
        AppDbContext db,
        IOcrService ocr,
        IEncryptionService encryption,
        IFileStorageService storage,
        INotificationService notifications,
        IOptions<KycSettings> settings,
        IOptions<MinioSettings> minio,
        ILogger<KycService> logger)
    {
        _db = db;
        _ocr = ocr;
        _encryption = encryption;
        _storage = storage;
        _notifications = notifications;
        _settings = settings.Value;
        _minio = minio.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<KycResultDto>> SubmitAsync(string userId, Stream imageStream,
        string fileName, string contentType, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return ServiceResult<KycResultDto>.Failure("Usuario no encontrado.");
        if (user.IsKycVerified)
            return ServiceResult<KycResultDto>.Failure("La identidad ya fue validada.");

        // 1. Leer la imagen en memoria.
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, ct);
        var imageBytes = ms.ToArray();

        // 2. Cifrar y guardar en bucket privado (protección criptográfica en reposo).
        string? objectKey = null;
        try
        {
            var encrypted = _encryption.Encrypt(imageBytes);
            using var encStream = new MemoryStream(encrypted);
            objectKey = await _storage.UploadAsync(_minio.PrivateBucket, encStream, $"{fileName}.enc",
                "application/octet-stream", publicRead: false, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo almacenar el documento cifrado del usuario {UserId}", userId);
        }

        // 3. OCR + extracción de campos.
        var text = _ocr.ExtractText(imageBytes);
        var extraction = text is null
            ? new KycExtraction(null, null, null, null)
            : KycDocumentParser.Parse(text);

        // 4. Veredicto.
        var (status, reason) = EvaluateVerdict(text, extraction);

        var verification = new KycVerification
        {
            UserId = userId,
            FirstName = extraction.FirstName,
            LastName = extraction.LastName,
            DocumentNumber = extraction.DocumentNumber,
            BirthDate = extraction.BirthDate,
            Status = status,
            DocumentObjectKey = objectKey,
            RejectionReason = reason,
            ProcessedAt = DateTime.UtcNow
        };
        _db.KycVerifications.Add(verification);

        if (status == KycStatus.Approved)
        {
            user.IsKycVerified = true;
            // Completar el perfil con los datos validados si están vacíos.
            user.FirstName ??= extraction.FirstName;
            user.LastName ??= extraction.LastName;
        }

        await _db.SaveChangesAsync(ct);

        // 5. Eliminación segura: emitido el veredicto, el documento ya no es necesario.
        if (objectKey is not null)
        {
            try
            {
                await _storage.DeleteAsync(_minio.PrivateBucket, objectKey, ct);
                verification.DocumentObjectKey = null;
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo eliminar el documento KYC {Key}", objectKey);
            }
        }

        // 6. Notificar el veredicto.
        await _notifications.NotifyAsync(userId,
            status == KycStatus.Approved ? NotificationType.KycApproved : NotificationType.KycRejected,
            status == KycStatus.Approved ? "Identidad verificada" : "Validación de identidad rechazada",
            status == KycStatus.Approved
                ? "Tu identidad fue validada. Ya puedes realizar reservas."
                : $"No pudimos validar tu identidad: {reason}",
            ct: ct);

        _logger.LogInformation("KYC de {UserId} -> {Status}", userId, status);

        return ServiceResult<KycResultDto>.Success(new KycResultDto
        {
            Status = status.ToString(),
            IsKycVerified = user.IsKycVerified,
            FirstName = extraction.FirstName,
            LastName = extraction.LastName,
            DocumentNumber = extraction.DocumentNumber,
            BirthDate = extraction.BirthDate,
            RejectionReason = reason
        });
    }

    public async Task<KycStatusDto> GetStatusAsync(string userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        var last = await _db.KycVerifications.AsNoTracking()
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new KycStatusDto
        {
            Status = last?.Status.ToString() ?? "NotStarted",
            IsKycVerified = user?.IsKycVerified ?? false,
            ProcessedAt = last?.ProcessedAt
        };
    }

    /// <summary>Reglas del veredicto automático.</summary>
    private (KycStatus Status, string? Reason) EvaluateVerdict(string? ocrText, KycExtraction e)
    {
        if (ocrText is null)
            return (KycStatus.Rejected, "No se pudo procesar la imagen del documento.");

        if (string.IsNullOrWhiteSpace(e.DocumentNumber))
            return (KycStatus.Rejected, "No se pudo leer el número de documento.");

        // La fecha de nacimiento puede estar en el reverso; si no se detectó, se omite el chequeo de edad.
        if (e.BirthDate is not null)
        {
            var age = AgeOn(e.BirthDate.Value, DateOnly.FromDateTime(DateTime.UtcNow));
            if (age < _settings.MinimumAge)
                return (KycStatus.Rejected, $"El titular debe ser mayor de {_settings.MinimumAge} años.");
        }

        return (KycStatus.Approved, null);
    }

    private static int AgeOn(DateOnly birth, DateOnly today)
    {
        var age = today.Year - birth.Year;
        if (birth > today.AddYears(-age)) age--;
        return age;
    }
}
