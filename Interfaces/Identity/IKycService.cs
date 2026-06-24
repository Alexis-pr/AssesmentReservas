using AssesmentReservas.API.Common;
using AssesmentReservas.API.DTOs.Identity;

namespace AssesmentReservas.API.Interfaces.Identity;

/// <summary>Validación de identidad (KYC) asistida por OCR.</summary>
public interface IKycService
{
    Task<ServiceResult<KycResultDto>> SubmitAsync(string userId, Stream imageStream,
        string fileName, string contentType, CancellationToken ct = default);

    Task<KycStatusDto> GetStatusAsync(string userId, CancellationToken ct = default);
}
