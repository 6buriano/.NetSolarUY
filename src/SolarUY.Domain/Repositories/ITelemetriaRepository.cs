using SolarUY.Domain.Entities;

namespace SolarUY.Domain.Repositories;

/// <summary>
/// Contrato de persistencia de las lecturas de telemetría que genera el simulador.
/// </summary>
public interface ITelemetriaRepository
{
    Task AgregarAsync(LecturaTelemetria lectura, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda todas las lecturas de una ronda en un solo <c>SaveChanges</c>. El colector consulta
    /// un nodo entero por vez, así que guardar de a una sería un viaje a la base por panel.
    /// </summary>
    Task AgregarVariasAsync(IReadOnlyList<LecturaTelemetria> lecturas, CancellationToken cancellationToken = default);

    /// <summary>Devuelve las lecturas de un panel dentro de un rango de fechas, de la más reciente a la más antigua.</summary>
    Task<IReadOnlyList<LecturaTelemetria>> ObtenerPorPanelAsync(Guid panelSolarId, DateTime desde, DateTime hasta,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve las últimas lecturas de un panel, de la más reciente a la más antigua.
    /// Con <paramref name="cantidad"/> en 1 sirve para saber la última lectura; con 20, para
    /// mostrar la serie reciente en pantalla.
    /// </summary>
    Task<IReadOnlyList<LecturaTelemetria>> ObtenerUltimasPorPanelAsync(Guid panelSolarId, int cantidad,
        CancellationToken cancellationToken = default);
}
