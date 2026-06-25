namespace AssesmentReservas.API.Interfaces.Infrastructure;

/// <summary>Cifrado simétrico autenticado para datos sensibles (documentos KYC).</summary>
public interface IEncryptionService
{
    byte[] Encrypt(byte[] plaintext);
    byte[] Decrypt(byte[] payload);
}
