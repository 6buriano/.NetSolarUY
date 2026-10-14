namespace SolarUY.Web.Helpers;

/// <summary>
/// Convierte las horas UTC que reporta el sensor a la hora de Montevideo para mostrarlas.
/// </summary>
/// <remarks>
/// El dominio, el contrato del sensor y la base guardan siempre UTC; la conversión es solo de
/// presentación. Se usa la zona horaria del sistema en lugar de restar 3 horas fijas porque
/// Uruguay tuvo horario de verano hasta 2015 y así queda explícito cuál es la regla.
/// </remarks>
public static class HoraLocal
{
    public const string NombreZona = "America/Montevideo";

    private static readonly TimeZoneInfo Zona = ResolverZona();

    /// <summary>Etiqueta para los encabezados, así queda claro en qué hora están.</summary>
    public static string Etiqueta => "hora de Montevideo (UTC-3)";

    public static DateTime DesdeUtc(DateTime fechaUtc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc), Zona);

    private static TimeZoneInfo ResolverZona()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(NombreZona);
        }
        catch (Exception excepcion) when (excepcion is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Si el sistema no tiene la base de zonas horarias, Uruguay es UTC-3 fijo.
            return TimeZoneInfo.CreateCustomTimeZone("UY-3", TimeSpan.FromHours(-3), NombreZona, NombreZona);
        }
    }
}
