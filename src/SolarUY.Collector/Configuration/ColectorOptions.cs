using SolarUY.Application.Services;

namespace SolarUY.Collector.Configuration;

/// <summary>
/// Opciones de la sección "Colector" de appsettings.json (spec del simulador, sección 8).
/// </summary>
public sealed class ColectorOptions
{
    public const string Seccion = "Colector";

    /// <summary>
    /// Si está en false, el worker no consulta nada. Sirve para dejar el colector apagado en
    /// todas las instancias menos una cuando la web corre en varios nodos.
    /// </summary>
    public bool Habilitado { get; set; } = true;

    /// <summary>Cada cuántos segundos se consulta a todos los sensores.</summary>
    public int IntervaloSegundos { get; set; } = 5;

    /// <summary>
    /// Si está en false, no se purga la telemetría vieja. Sirve para una demo donde se quieren
    /// conservar todas las lecturas.
    /// </summary>
    public bool RetencionHabilitada { get; set; } = true;

    /// <summary>
    /// Cuántos días de lecturas se conservan. El colector escribe unas 260.000 filas por día, así
    /// que sin esto la tabla (y el disco del servidor) crecen sin límite.
    /// </summary>
    public int RetencionDias { get; set; } = RetencionTelemetriaService.DiasPorDefecto;

    /// <summary>Cada cuántas horas se ejecuta la purga. No hace falta más seguido que eso.</summary>
    public int IntervaloRetencionHoras { get; set; } = 6;
}
