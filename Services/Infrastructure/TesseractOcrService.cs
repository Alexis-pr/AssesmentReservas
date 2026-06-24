using AssesmentReservas.API.Interfaces.Infrastructure;
using AssesmentReservas.API.Settings;
using Microsoft.Extensions.Options;
using Tesseract;

namespace AssesmentReservas.API.Services.Infrastructure;

/// <summary>OCR local con Tesseract (los binarios nativos y los datos entrenados se instalan en Docker).</summary>
public class TesseractOcrService : IOcrService
{
    private readonly KycSettings _settings;
    private readonly ILogger<TesseractOcrService> _logger;

    public TesseractOcrService(IOptions<KycSettings> settings, ILogger<TesseractOcrService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public string? ExtractText(byte[] imageBytes)
    {
        try
        {
            // TesseractEngine no es thread-safe: se crea uno por invocación.
            using var engine = new TesseractEngine(_settings.TessDataPath, _settings.Languages, EngineMode.Default);
            using var pix = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix);
            return page.GetText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el OCR de Tesseract (TessData: {Path}, Langs: {Langs})",
                _settings.TessDataPath, _settings.Languages);
            return null;
        }
    }
}
