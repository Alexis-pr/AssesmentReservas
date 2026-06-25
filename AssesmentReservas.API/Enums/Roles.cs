namespace AssesmentReservas.API.Enums;

/// <summary>Roles del sistema. Se usan como constantes para [Authorize(Roles = ...)].</summary>
public static class Roles
{
    /// <summary>Arrendatario / Huésped.</summary>
    public const string Guest = "Guest";

    /// <summary>Propietario / Anfitrión.</summary>
    public const string Owner = "Owner";

    public static readonly string[] All = [Guest, Owner];
}
