namespace AssesmentReservas.API.Interfaces.Infrastructure;

/// <summary>Reconocimiento óptico de caracteres sobre una imagen.</summary>
public interface IOcrService
{
    /// <summary>Extrae el texto plano de la imagen. Devuelve null si el OCR no está disponible.</summary>
    string? ExtractText(byte[] imageBytes);
}
