using System.Diagnostics;
using AssesmentReservas.API.Interfaces.Infrastructure;
using AssesmentReservas.API.Settings;
using Microsoft.Extensions.Options;

namespace AssesmentReservas.API.Services.Infrastructure;

/// <summary>
/// OCR usando el ejecutable `tesseract` CLI, evitando los problemas de resolución
/// de librerías nativas del binding P/Invoke en contenedores Linux.
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
        var tmpInput = Path.Combine(Path.GetTempPath(), $"kyc_{Guid.NewGuid():N}.png");
        try
        {
            File.WriteAllBytes(tmpInput, imageBytes);

            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "tesseract",
                    // "stdout" hace que tesseract escriba el texto en stdout en lugar de un archivo.
                    Arguments = $"\"{tmpInput}\" stdout -l {_settings.Languages} --psm 6",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                }
            };

            proc.Start();
            var text  = proc.StandardOutput.ReadToEnd();
            var err   = proc.StandardError.ReadToEnd();
            proc.WaitForExit(30_000);

            if (proc.ExitCode != 0)
            {
                _logger.LogError("Tesseract CLI falló (exit {Code}): {Err}", proc.ExitCode, err);
                return null;
            }

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invocando tesseract CLI (Langs: {Langs})", _settings.Languages);
            return null;
        }
        finally
        {
            try { File.Delete(tmpInput); } catch { /* best-effort */ }
        }
    }
}
