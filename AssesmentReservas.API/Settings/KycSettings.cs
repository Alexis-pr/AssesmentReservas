namespace AssesmentReservas.API.Settings;

/// <summary>Configuración del módulo de validación de identidad (KYC) por OCR local.</summary>
public class KycSettings
{
    public const string SectionName = "Kyc";

    /// <summary>Ruta a los datos entrenados de Tesseract (eng/spa).</summary>
    public string TessDataPath { get; set; } = "/usr/share/tesseract-ocr/5/tessdata";

    /// <summary>Idiomas de OCR (ej: "spa+eng").</summary>
    public string Languages { get; set; } = "spa+eng";

    /// <summary>Clave de cifrado (Base64, 32 bytes) para los documentos antes de subirlos a MinIO.</summary>
    public string EncryptionKey { get; set; } = default!;

    /// <summary>Edad mínima requerida para aprobar automáticamente.</summary>
    public int MinimumAge { get; set; } = 18;
}
