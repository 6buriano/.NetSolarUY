using Microsoft.Extensions.Options;
using SolarUY.Application.Sensors;
using SolarUY.Collector.Configuration;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Repositories;

namespace SolarUY.Collector;

/// <summary>
/// Colector de telemetría (spec del simulador, sección 8). En cada ronda consulta por HTTP el
/// estado de cada sensor activo, le pide al dominio que diagnostique las lecturas y las guarda
/// en MySQL. Para el colector el sensor es una caja negra: da igual si es el simulador o
/// hardware real, lo único que cambia es la dirección.
/// </summary>
public class ColectorTelemetriaWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ColectorOptions _opciones;
    private readonly ILogger<ColectorTelemetriaWorker> _logger;

    public ColectorTelemetriaWorker(IServiceScopeFactory scopeFactory, IOptions<ColectorOptions> opciones,
        ILogger<ColectorTelemetriaWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _opciones = opciones.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opciones.Habilitado)
        {
            _logger.LogInformation(
                "El colector de telemetría está apagado por configuración (Colector:Habilitado = false).");
            return;
        }

        var intervalo = TimeSpan.FromSeconds(Math.Max(1, _opciones.IntervaloSegundos));
        _logger.LogInformation("Colector de telemetría iniciado: una ronda cada {Segundos} segundos.",
            intervalo.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecolectarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception excepcion)
            {
                // Que falle una ronda (por ejemplo, porque MySQL no responde) no puede matar al worker.
                _logger.LogError(excepcion, "Falló la ronda de recolección de telemetría.");
            }

            try
            {
                await Task.Delay(intervalo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RecolectarAsync(CancellationToken cancellationToken)
    {
        // Un scope por ronda: el DbContext y los repositorios son scoped.
        using var scope = _scopeFactory.CreateScope();
        var nodoRepository = scope.ServiceProvider.GetRequiredService<INodoRepository>();
        var telemetriaRepository = scope.ServiceProvider.GetRequiredService<ITelemetriaRepository>();
        var sensorClient = scope.ServiceProvider.GetRequiredService<ISensorClient>();

        var nodos = await nodoRepository.ObtenerConSensoresActivosAsync(cancellationToken);

        if (nodos.Count == 0)
        {
            _logger.LogDebug("Todavía no hay nodos con sensor activo para consultar.");
            return;
        }

        var lecturasGuardadas = 0;

        foreach (var nodo in nodos)
        {
            // Cada sensor se consulta por separado: si uno no responde, los demás siguen.
            var sensor = nodo.Inversor.Sensor!;
            var estado = await sensorClient.ObtenerEstadoAsync(sensor.UrlBase, sensor.IdentificadorHardwareARM,
                cancellationToken);

            if (estado is null)
            {
                _logger.LogWarning("El sensor {Hardware} del nodo {Nodo} no respondió: se salta en esta ronda.",
                    sensor.IdentificadorHardwareARM, nodo.Nombre);
                continue;
            }

            var lecturas = ArmarLecturas(nodo, estado);
            if (lecturas.Count == 0)
            {
                continue;
            }

            // El sensor sólo mide: el diagnóstico lo hace el dominio.
            nodo.EvaluarLecturas(lecturas);

            // El sensor también es la fuente de verdad del volcado a la red.
            nodo.Inversor.EstablecerVolcadoRed(estado.VolcadoRedActivo);

            // Primero los paneles (estado y actividad) y después las lecturas ya diagnosticadas.
            await nodoRepository.ActualizarAsync(nodo, cancellationToken);
            await telemetriaRepository.AgregarVariasAsync(lecturas, cancellationToken);

            lecturasGuardadas += lecturas.Count;
        }

        if (lecturasGuardadas > 0)
        {
            _logger.LogInformation("Ronda de telemetría: {Cantidad} lecturas guardadas.", lecturasGuardadas);
        }
    }

    /// <summary>
    /// Empareja lo que informa el sensor con los paneles del dominio por código de panel, y
    /// sincroniza si el panel está habilitado antes de diagnosticar (la actividad influye en el
    /// diagnóstico: un panel inactivo queda DESCONECTADO).
    /// </summary>
    private List<LecturaTelemetria> ArmarLecturas(Nodo nodo, EstadoSensorDto estado)
    {
        var lecturas = new List<LecturaTelemetria>(estado.Paneles.Count);

        foreach (var lecturaSensor in estado.Paneles)
        {
            var panel = nodo.BuscarPanelPorCodigo(lecturaSensor.CodigoPanel);

            if (panel is null)
            {
                _logger.LogWarning(
                    "El sensor {Hardware} informa el panel '{Codigo}', que no existe en el nodo {Nodo}. Se ignora.",
                    estado.IdentificadorHardwareARM, lecturaSensor.CodigoPanel, nodo.Nombre);
                continue;
            }

            panel.MarcarActividad(lecturaSensor.Activo);

            lecturas.Add(new LecturaTelemetria(
                panel.Id,
                estado.FechaHoraUtc,
                lecturaSensor.CorrienteContinua,
                lecturaSensor.Voltaje,
                lecturaSensor.PotenciaGeneradaWatts,
                lecturaSensor.TemperaturaCelsius));
        }

        return lecturas;
    }
}
