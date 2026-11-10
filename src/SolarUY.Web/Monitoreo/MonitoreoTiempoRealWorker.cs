using Microsoft.AspNetCore.SignalR;
using SolarUY.Application.Services;
using SolarUY.Web.Hubs;

namespace SolarUY.Web.Monitoreo;

/// <summary>
/// Cada <c>Monitoreo:IntervaloSegundos</c> (5 por defecto) arma la foto del estado actual y la
/// envía por SignalR a las pantallas abiertas (req012). Corre dentro de la web: el Collector no se
/// toca. Las dos réplicas lo corren, pero solo reciben datos los navegadores conectados a la que
/// está atendiendo.
/// </summary>
public class MonitoreoTiempoRealWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MonitoreoHub> _hub;
    private readonly SeguimientoMonitoreo _seguimiento;
    private readonly ILogger<MonitoreoTiempoRealWorker> _logger;
    private readonly bool _habilitado;
    private readonly TimeSpan _intervalo;

    public MonitoreoTiempoRealWorker(IServiceScopeFactory scopeFactory, IHubContext<MonitoreoHub> hub,
        SeguimientoMonitoreo seguimiento, ILogger<MonitoreoTiempoRealWorker> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _seguimiento = seguimiento;
        _logger = logger;
        _habilitado = configuration.GetValue("Monitoreo:Habilitado", true);
        _intervalo = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("Monitoreo:IntervaloSegundos", 5)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_habilitado)
        {
            _logger.LogInformation("Monitoreo en tiempo real deshabilitado (Monitoreo:Habilitado = false).");
            return;
        }

        using var timer = new PeriodicTimer(_intervalo);
        do
        {
            try
            {
                await EnviarAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Una vuelta que falla (por ejemplo, MySQL no responde) se registra y se sigue.
                _logger.LogError(ex, "Falló una vuelta del monitoreo en tiempo real.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EnviarAsync(CancellationToken cancellationToken)
    {
        // El worker vive todo el tiempo (Singleton) y los repositorios son Scoped: se crea un
        // scope por vuelta para tener un DbContext nuevo cada vez.
        using var scope = _scopeFactory.CreateScope();
        var monitoreoService = scope.ServiceProvider.GetRequiredService<MonitoreoService>();

        var (instantanea, lecturas) = await monitoreoService.ObtenerInstantaneaAsync(cancellationToken);
        var cambios = _seguimiento.Comparar(instantanea, lecturas);

        var grupoMonitoreo = _hub.Clients.Group(MonitoreoHub.GrupoMonitoreo);
        await grupoMonitoreo.SendAsync(MonitoreoHub.MensajeInstantanea, instantanea, cancellationToken);

        foreach (var falla in cambios.Fallas)
        {
            await grupoMonitoreo.SendAsync(MonitoreoHub.MensajeFalla, falla, cancellationToken);
        }

        foreach (var lectura in cambios.LecturasNuevas)
        {
            await _hub.Clients.Group(MonitoreoHub.GrupoPanel(lectura.PanelSolarId))
                .SendAsync(MonitoreoHub.MensajeLectura, lectura, cancellationToken);
        }
    }
}