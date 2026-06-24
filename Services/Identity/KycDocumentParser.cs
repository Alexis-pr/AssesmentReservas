using System.Globalization;
using System.Text.RegularExpressions;

namespace AssesmentReservas.API.Services.Identity;

/// <summary>Datos extraídos del documento por OCR.</summary>
public record KycExtraction(string? FirstName, string? LastName, string? DocumentNumber, DateOnly? BirthDate);

/// <summary>
/// Parser heurístico de cédula colombiana a partir del texto OCR. El OCR es ruidoso,
/// por lo que se combinan etiquetas conocidas con patrones (regex) como respaldo.
/// </summary>
public static partial class KycDocumentParser
{
    private static readonly Dictionary<string, int> SpanishMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ENE"] = 1, ["FEB"] = 2, ["MAR"] = 3, ["ABR"] = 4, ["MAY"] = 5, ["JUN"] = 6,
        ["JUL"] = 7, ["AGO"] = 8, ["SEP"] = 9, ["OCT"] = 10, ["NOV"] = 11, ["DIC"] = 12
    };

    public static KycExtraction Parse(string rawText)
    {
        var text = rawText.ToUpperInvariant();
        var lines = text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new KycExtraction(
            FirstName: ValueAfterLabel(lines, "NOMBRES"),
            LastName: ValueAfterLabel(lines, "APELLIDOS"),
            DocumentNumber: ExtractDocumentNumber(text),
            BirthDate: ExtractBirthDate(text));
    }

    /// <summary>Devuelve la línea siguiente (o el resto de la misma) a una etiqueta dada.</summary>
    private static string? ValueAfterLabel(string[] lines, string label)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var idx = lines[i].IndexOf(label, StringComparison.Ordinal);
            if (idx < 0) continue;

            var sameLine = lines[i][(idx + label.Length)..].Trim(' ', ':', '.');
            if (LooksLikeName(sameLine))
                return Titleize(sameLine);

            if (i + 1 < lines.Length && LooksLikeName(lines[i + 1]))
                return Titleize(lines[i + 1]);
        }
        return null;
    }

    private static bool LooksLikeName(string s) =>
        s.Length is >= 2 and <= 60 && NameRegex().IsMatch(s);

    private static string? ExtractDocumentNumber(string text)
    {
        // Números tipo 1.234.567.890 / 1 234 567 890 / 1234567890
        var matches = NumberRegex().Matches(text);
        return matches
            .Select(m => new string(m.Value.Where(char.IsDigit).ToArray()))
            .Where(n => n.Length is >= 7 and <= 11)
            .OrderByDescending(n => n.Length)
            .FirstOrDefault();
    }

    private static DateOnly? ExtractBirthDate(string text)
    {
        var dates = new List<DateOnly>();

        // dd MMM yyyy con mes en español (ej: 15-ENE-1990)
        foreach (Match m in MonthDateRegex().Matches(text))
        {
            if (SpanishMonths.TryGetValue(m.Groups[2].Value, out var month) &&
                TryBuildDate(int.Parse(m.Groups[3].Value), month, int.Parse(m.Groups[1].Value), out var d))
                dates.Add(d);
        }

        // dd/mm/yyyy o yyyy/mm/dd
        foreach (Match m in NumericDateRegex().Matches(text))
        {
            var a = int.Parse(m.Groups[1].Value);
            var b = int.Parse(m.Groups[2].Value);
            var c = int.Parse(m.Groups[3].Value);

            if (m.Groups[1].Value.Length == 4)
            {
                if (TryBuildDate(a, b, c, out var d1)) dates.Add(d1);
            }
            else if (TryBuildDate(c, b, a, out var d2))
            {
                dates.Add(d2);
            }
        }

        // De varias fechas, la de nacimiento suele ser la más antigua (vs. expedición/vigencia).
        return dates.Count == 0 ? null : dates.MinBy(d => d.DayNumber);
    }

    private static bool TryBuildDate(int year, int month, int day, out DateOnly date)
    {
        date = default;
        if (year is < 1900 or > 2100 || month is < 1 or > 12 || day is < 1 or > 31)
            return false;
        try { date = new DateOnly(year, month, day); return true; }
        catch { return false; }
    }

    private static string Titleize(string value)
    {
        var clean = WhitespaceRegex().Replace(value.Trim(), " ");
        return CultureInfo.GetCultureInfo("es-CO").TextInfo.ToTitleCase(clean.ToLowerInvariant());
    }

    [GeneratedRegex(@"^[A-ZÁÉÍÓÚÑ][A-ZÁÉÍÓÚÑ\s]+$")]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"\d{1,3}(?:[.\s]\d{3}){2,3}|\d{7,11}")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"(\d{1,2})[-/.\s]([A-Z]{3})[-/.\s](\d{4})")]
    private static partial Regex MonthDateRegex();

    [GeneratedRegex(@"(\d{1,4})[-/.](\d{1,2})[-/.](\d{1,4})")]
    private static partial Regex NumericDateRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
