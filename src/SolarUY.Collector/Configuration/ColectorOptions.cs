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
}
