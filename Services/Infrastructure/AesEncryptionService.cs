using System.Security.Cryptography;
using AssesmentReservas.API.Interfaces.Infrastructure;
using AssesmentReservas.API.Settings;
using Microsoft.Extensions.Options;

namespace AssesmentReservas.API.Services.Infrastructure;

/// <summary>
/// Cifrado AES-256-GCM (autenticado). El payload resultante es: nonce(12) | tag(16) | ciphertext.
/// La clave (32 bytes) se inyecta en Base64 vía configuración (Kyc:EncryptionKey).
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public AesEncryptionService(IOptions<KycSettings> settings)
    {
        _key = Convert.FromBase64String(settings.Value.EncryptionKey);
        if (_key.Length != 32)
            throw new InvalidOperationException("Kyc:EncryptionKey debe ser de 32 bytes (256 bits) en Base64.");
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);
        return payload;
    }

    public byte[] Decrypt(byte[] payload)
    {
        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
