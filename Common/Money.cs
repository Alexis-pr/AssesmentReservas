using System.Globalization;

namespace AssesmentReservas.API.Common;

/// <summary>
/// Formateo de moneda en es-CO (COP) independiente de la cultura del servidor.
/// Solo afecta la presentación; el binding de formularios sigue siendo invariante.
/// </summary>
public static class Money
{
    private static readonly CultureInfo Co = CultureInfo.GetCultureInfo("es-CO");

    public static string Cop(this decimal amount) => amount.ToString("C0", Co);
}
