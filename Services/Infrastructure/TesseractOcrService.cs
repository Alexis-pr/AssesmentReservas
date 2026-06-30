using System.Diagnostics;
using AssesmentReservas.API.Interfaces.Infrastructure;
using AssesmentReservas.API.Settings;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace AssesmentReservas.API.Services.Infrastructure;

/// <summary>
/// OCR usando el ejecutable <c>tesseract</c> CLI.
/// Antes de invocar tesseract convierte la imagen a PNG escala de grises con ImageSharp
/// para garantizar compatibilidad independientemente del formato original (webp, jpg, heic…)
/// y mejorar la tasa de reconocimiento en documentos de identidad.
/// </summary>
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
        var id        = Guid.NewGuid().ToString("N");
        var tmpInput  = Path.Combine(Path.GetTempPath(), $"kyc_{id}_in.png");
        var tmpBase   = Path.Combine(Path.GetTempPath(), $"kyc_{id}_out");
        var tmpOutput = tmpBase + ".txt";

        try
        {
            // 1. Convertir a PNG gris normalizado (ImageSharp soporta webp, jpg, png, bmp, gif…)
            using (var ms = new MemoryStream(imageBytes))
            using (var img = Image.Load(ms))
            {
                img.Mutate(x => x
                    .AutoOrient()                         // respetar EXIF rotation
                    .Grayscale()                          // mejor contraste para OCR
                    .Resize(new ResizeOptions              // escalar si es muy pequeña
                    {
                        Mode    = ResizeMode.Min,
                        Size    = new Size(1200, 0)
                    }));
                img.SaveAsPng(tmpInput);
            }

            // 2. Invocar tesseract CLI: escribe resultado en <tmpBase>.txt
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName  = "tesseract",
                    Arguments = $"\"{tmpInput}\" \"{tmpBase}\" -l {_settings.Languages} --psm 6",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                }
            };

            proc.Start();
            // Leer stderr en paralelo para evitar deadlock en buffers llenos
            var errTask = proc.StandardError.ReadToEndAsync();
            proc.WaitForExit(30_000);

            if (proc.ExitCode != 0)
            {
                var err = errTask.GetAwaiter().GetResult();
                _logger.LogError("Tesseract CLI falló (exit {Code}): {Err}", proc.ExitCode, err);
                return null;
            }

            if (!File.Exists(tmpOutput))
            {
                _logger.LogWarning("Tesseract no generó archivo de salida: {Path}", tmpOutput);
                return null;
            }

            var text = File.ReadAllText(tmpOutput);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invocando tesseract CLI (Langs: {Langs})", _settings.Languages);
            return null;
        }
        finally
        {
            try { File.Delete(tmpInput);  } catch { /* best-effort */ }
            try { File.Delete(tmpOutput); } catch { /* best-effort */ }
        }
    }
}
