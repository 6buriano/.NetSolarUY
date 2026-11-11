using SolarUY.Application.DTOs;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Services;

/// <summary>
/// Arma la "foto" del estado actual de los parques para el monitoreo en tiempo real (req012):
/// potencia actual del parque, de cada nodo y de cada panel, con el estado de salud de cada panel.
/// </summary>
public class MonitoreoService
{
    /// <summary>
    /// Cuántos segundos hacia atrás se buscan lecturas: 3 rondas del colector (cada 5 s).
    /// Si un panel no tiene lectura en ese lapso, se muestra "sin lectura reciente".
    /// </summary>
    public const int VentanaSegundos = 15;

    private readonly IParqueRepository _parqueRepository;
    private readonly ITelemetriaRepository _telemetriaRepository;

    public MonitoreoService(IParqueRepository parqueRepository, ITelemetriaRepository telemetriaRepository)
    {
        _parqueRepository = parqueRepository;
        _telemetriaRepository = telemetriaRepository;
    }

    /// <summary>
    /// Devuelve la foto actual y también las lecturas de la ventana, que el worker usa para
    /// detectar cuáles son nuevas y mandarlas al detalle de cada panel.
    /// </summary>
    public async Task<(InstantaneaMonitoreoDto Instantanea, IReadOnlyList<LecturaTelemetria> Lecturas)>
        ObtenerInstantaneaAsync(CancellationToken cancellationToken = default)
    {
        var ahoraUtc = DateTime.UtcNow;
        var parques = await _parqueRepository.ObtenerTodosAsync(cancellationToken);
        var lecturas = await _telemetriaRepository.ObtenerDesdeAsync(
            ahoraUtc.AddSeconds(-VentanaSegundos), cancellationToken);

        return (ArmarInstantanea(parques, lecturas, ahoraUtc), lecturas);
    }

    /// <summary>
    /// Arma la foto a partir de los parques y las lecturas recientes. Es pura (no usa la base),
    /// así se puede probar con datos inventados.
    /// </summary>
    public static InstantaneaMonitoreoDto ArmarInstantanea(IReadOnlyList<ParqueSolar> parques,
        IReadOnlyList<LecturaTelemetria> lecturas, DateTime ahoraUtc)
    {
        // De todas las lecturas de la ventana, la más reciente de cada panel.
        var ultimaPorPanel = lecturas
            .GroupBy(lectura => lectura.PanelSolarId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.MaxBy(lectura => lectura.FechaHora)!);

        var parquesDto = parques
            .OrderBy(parque => parque.Nombre)
            .Select(parque =>
            {
                var nodos = parque.Nodos
                    .OrderBy(nodo => nodo.Nombre)
                    .Select(nodo => ArmarNodo(nodo, ultimaPorPanel))
                    .ToList();

                return new MonitoreoParqueDto(parque.Id, parque.Nombre, nodos.Sum(nodo => nodo.PotenciaActualW), nodos);
            })
            .ToList();

        return new InstantaneaMonitoreoDto(ahoraUtc, parquesDto);
    }

    private static MonitoreoNodoDto ArmarNodo(Nodo nodo, Dictionary<Guid, LecturaTelemetria> ultimaPorPanel)
    {
        var paneles = nodo.Paneles
            .OrderBy(panel => panel.CodigoPanel)
            .Select(panel =>
            {
                ultimaPorPanel.TryGetValue(panel.Id, out var lectura);

                return new MonitoreoPanelDto(panel.Id, panel.CodigoPanel, panel.Estado, panel.Activo,
                    lectura?.PotenciaGeneradaWatts, lectura?.TemperaturaCelsius, lectura?.FechaHora);
            })
            .ToList();

        // Suman solo los paneles activos que tienen una lectura reciente.
        var potencia = paneles
            .Where(panel => panel.Activo && panel.PotenciaActualW is not null)
            .Sum(panel => panel.PotenciaActualW!.Value);

        return new MonitoreoNodoDto(nodo.Id, nodo.Nombre, potencia, paneles);
    }
}