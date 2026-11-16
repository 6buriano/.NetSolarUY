using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;
using SolarUY.Web.Hubs;
using SolarUY.Web.Monitoreo;

namespace SolarUY.Web.Tests;

/// <summary>
/// Pruebas del worker de monitoreo en tiempo real. Se usa el <see cref="MonitoreoService"/> real, un hub
/// falso que anota lo que se manda a cada grupo y repositorios falsos. Se espera con un sondeo corto y un
/// tope, así que una prueba nunca queda colgada.
/// </summary>
public class MonitoreoTiempoRealWorkerTests
{
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(15);
    private static readonly DateTime Base = DateTime.UtcNow;

    private static MonitoreoTiempoRealWorker CrearWorker(HubContextFalso hub, ParqueSolar[] parques,
        ITelemetriaRepository telemetria, bool habilitado = true, ILogger<MonitoreoTiempoRealWorker>? logger = null)
    {
        var servicios = new ServiceCollection();
        servicios.AddSingleton<IParqueRepository>(new ParqueRepositoryMonitoreoFalso(parques));
        servicios.AddSingleton(telemetria);
        servicios.AddScoped<MonitoreoService>();
        var proveedor = servicios.BuildServiceProvider();

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Monitoreo:Habilitado"] = habilitado.ToString(),
                // El mínimo que acepta el worker: la segunda vuelta llega al segundo.
                ["Monitoreo:IntervaloSegundos"] = "1"
            })
            .Build();

        return new MonitoreoTiempoRealWorker(proveedor.GetRequiredService<IServiceScopeFactory>(), hub,
            new SeguimientoMonitoreo(), logger ?? NullLogger<MonitoreoTiempoRealWorker>.Instance, configuracion);
    }

    [Fact]
    public async Task Si_esta_deshabilitado_no_envia_nada_y_termina()
    {
        var hub = new HubContextFalso();
        using var worker = CrearWorker(hub, [FixtureMonitoreo.Parque()], new TelemetriaRepositoryMonitoreoFalso(),
            habilitado: false);

        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(EsperaMaxima);

        Assert.Empty(hub.Envios);
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task En_la_primera_vuelta_envia_la_foto_al_grupo_de_monitoreo()
    {
        var parque = FixtureMonitoreo.Parque();
        var hub = new HubContextFalso();
        using var worker = CrearWorker(hub, [parque], new TelemetriaRepositoryMonitoreoFalso());

        await worker.StartAsync(CancellationToken.None);
        await hub.EsperarAsync(envio => envio.Metodo == MonitoreoHub.MensajeInstantanea, EsperaMaxima);
        await worker.StopAsync(CancellationToken.None);

        var envio = hub.Envios.First(e => e.Metodo == MonitoreoHub.MensajeInstantanea);
        Assert.Equal(MonitoreoHub.GrupoMonitoreo, envio.Grupo);
        var foto = Assert.IsType<InstantaneaMonitoreoDto>(Assert.Single(envio.Argumentos));
        Assert.Equal(parque.Id, Assert.Single(foto.Parques).ParqueId);
    }

    [Fact]
    public async Task En_la_segunda_vuelta_avisa_la_falla_nueva_y_manda_la_lectura_nueva_al_grupo_del_panel()
    {
        var parque = FixtureMonitoreo.Parque();
        var nodo = FixtureMonitoreo.Nodo(parque);
        var panel = FixtureMonitoreo.Panel(nodo, "P01");
        var hub = new HubContextFalso();
        var telemetria = new TelemetriaRepositoryMonitoreoFalso(consulta =>
        {
            if (consulta == 1)
            {
                return [FixtureMonitoreo.Lectura(panel, Base)];
            }

            // Desde la segunda vuelta el panel está en falla y hay una lectura posterior a la ya enviada.
            panel.CambiarEstado(EstadoSalud.FALLA_DETECTADA);
            return [FixtureMonitoreo.Lectura(panel, Base), FixtureMonitoreo.Lectura(panel, Base.AddSeconds(5))];
        });
        using var worker = CrearWorker(hub, [parque], telemetria);

        await worker.StartAsync(CancellationToken.None);
        await hub.EsperarAsync(envio => envio.Metodo == MonitoreoHub.MensajeFalla, EsperaMaxima);
        await hub.EsperarAsync(envio => envio.Metodo == MonitoreoHub.MensajeLectura, EsperaMaxima);
        await worker.StopAsync(CancellationToken.None);

        var falla = hub.Envios.First(e => e.Metodo == MonitoreoHub.MensajeFalla);
        Assert.Equal(MonitoreoHub.GrupoMonitoreo, falla.Grupo);
        Assert.Equal("P01", Assert.IsType<AlertaFallaDto>(Assert.Single(falla.Argumentos)).CodigoPanel);

        var lectura = hub.Envios.First(e => e.Metodo == MonitoreoHub.MensajeLectura);
        Assert.Equal(MonitoreoHub.GrupoPanel(panel.Id), lectura.Grupo);
        Assert.Equal(panel.Id, Assert.IsType<LecturaTelemetriaDto>(Assert.Single(lectura.Argumentos)).PanelSolarId);
    }

    [Fact]
    public async Task Si_una_vuelta_falla_registra_el_error_y_el_worker_sigue_vivo()
    {
        var hub = new HubContextFalso();
        var telemetria = new TelemetriaRepositoryMonitoreoFalso(_ => throw new InvalidOperationException("MySQL no responde"));
        var logger = new LoggerQueEspera<MonitoreoTiempoRealWorker>(LogLevel.Error);
        using var worker = CrearWorker(hub, [FixtureMonitoreo.Parque()], telemetria, logger: logger);

        await worker.StartAsync(CancellationToken.None);
        await logger.Esperado.WaitAsync(EsperaMaxima);

        Assert.False(worker.ExecuteTask!.IsCompleted);
        await worker.StopAsync(CancellationToken.None);
        Assert.True(worker.ExecuteTask.IsCompleted);
        Assert.Empty(hub.Envios);
    }

    /// <summary>Logger que avisa (con un TaskCompletionSource) la primera vez que se registra el nivel buscado.</summary>
    private sealed class LoggerQueEspera<T>(LogLevel nivel) : ILogger<T>
    {
        private readonly TaskCompletionSource _esperado = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Esperado => _esperado.Task;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == nivel)
            {
                _esperado.TrySetResult();
            }
        }
    }
}
