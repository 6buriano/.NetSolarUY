using Microsoft.Extensions.Options;
using SolarUY.Application.Services;
using SolarUY.Collector.Configuration;

namespace SolarUY.Collector;

/// <summary>
/// Purga periódica de la telemetría vieja (política de retención). Corre en el colector y no en la
/// web porque el colector es una sola instancia: si lo hicieran las réplicas, cada una intentaría
/// borrar lo mismo y competirían por la tabla.
/// </summary>
public class RetencionTelemetriaWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ColectorOptions _opciones;
    private readonly ILogger<RetencionTelemetriaWorker> _logger;

    public RetencionTelemetriaWorker(IServiceScopeFactory scopeFactory, IOptions<ColectorOptions> opciones,
        ILogger<RetencionTelemetriaWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _opciones = opciones.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opciones.RetencionHabilitada)
        {
            _logger.LogInformation(
                "La purga de telemetría está apagada por configuración (Colector:RetencionHabilitada = false).");
            return;
        }

        if (_opciones.RetencionDias <= 0)
        {
            _logger.LogWarning(
                "Colector:RetencionDias tiene que ser mayor a cero (valor actual: {Dias}). La purga no se ejecuta.",
                _opciones.RetencionDias);
            return;
        }

        var intervalo = TimeSpan.FromHours(Math.Max(1, _opciones.IntervaloRetencionHoras));
        _logger.LogInformation(
            "Purga de telemetría iniciada: se conservan {Dias} días y corre una vez cada {Horas} horas.",
            _opciones.RetencionDias, intervalo.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception excepcion)
            {
                // Que falle una purga (por ejemplo, porque MySQL no responde) no puede matar al worker.
                _logger.LogError(excepcion, "Falló la purga de telemetría.");
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

    private async Task PurgarAsync(CancellationToken cancellationToken)
    {
        // Un scope por pasada: el DbContext y el repositorio son scoped.
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<RetencionTelemetriaService>();

        var borradas = await servicio.PurgarAsync(_opciones.RetencionDias, cancellationToken);

        if (borradas > 0)
        {
            _logger.LogInformation("Purga de telemetría: {Cantidad} filas borradas (lecturas de más de {Dias} días).",
                borradas, _opciones.RetencionDias);
        }
        else
        {
            _logger.LogDebug("Purga de telemetría: no había lecturas para borrar.");
        }
    }
}
