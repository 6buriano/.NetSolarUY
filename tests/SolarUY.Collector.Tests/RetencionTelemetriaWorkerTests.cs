using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SolarUY.Application.Services;
using SolarUY.Collector;
using SolarUY.Collector.Configuration;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Collector.Tests;

/// <summary>
/// Pruebas del worker de retención. Se usa el servicio real (<see cref="RetencionTelemetriaService"/>)
/// y un repositorio falso, y se espera a que el worker llame al repositorio con un
/// <see cref="TaskCompletionSource"/> en vez de dormir: así las pruebas no dependen de tiempos.
/// </summary>
public class RetencionTelemetriaWorkerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Si_la_retencion_esta_apagada_no_purga_y_el_worker_termina()
    {
        var repositorio = new RepositorioFalso();
        using var worker = CrearWorker(repositorio, new ColectorOptions { RetencionHabilitada = false });

        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(EsperaMaxima);

        Assert.Equal(0, repositorio.Llamadas);
        await worker.StopAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Si_los_dias_de_retencion_no_son_positivos_no_purga_y_el_worker_termina(int dias)
    {
        var repositorio = new RepositorioFalso();
        using var worker = CrearWorker(repositorio, new ColectorOptions { RetencionDias = dias });

        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(EsperaMaxima);

        Assert.Equal(0, repositorio.Llamadas);
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Purga_con_el_limite_calculado_a_partir_de_los_dias_configurados()
    {
        var repositorio = new RepositorioFalso { Borradas = 12 };
        using var worker = CrearWorker(repositorio, new ColectorOptions { RetencionDias = 30 });

        await worker.StartAsync(CancellationToken.None);
        await repositorio.PrimeraLlamada.WaitAsync(EsperaMaxima);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(Ahora.UtcDateTime.AddDays(-30), repositorio.UltimoLimite);
    }

    [Fact]
    public async Task Cuando_no_habia_nada_para_borrar_el_worker_sigue_andando()
    {
        var repositorio = new RepositorioFalso { Borradas = 0 };
        using var worker = CrearWorker(repositorio, new ColectorOptions());

        await worker.StartAsync(CancellationToken.None);
        await repositorio.PrimeraLlamada.WaitAsync(EsperaMaxima);

        // Quedó esperando el próximo intervalo: no terminó.
        Assert.False(worker.ExecuteTask!.IsCompleted);
        await worker.StopAsync(CancellationToken.None);
        Assert.True(worker.ExecuteTask.IsCompleted);
    }

    [Fact]
    public async Task Un_intervalo_en_cero_se_corrige_a_una_hora_y_la_purga_corre_igual()
    {
        var repositorio = new RepositorioFalso { Borradas = 3 };
        using var worker = CrearWorker(repositorio, new ColectorOptions { IntervaloRetencionHoras = 0 });

        await worker.StartAsync(CancellationToken.None);
        await repositorio.PrimeraLlamada.WaitAsync(EsperaMaxima);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(1, repositorio.Llamadas);
    }

    [Fact]
    public async Task Si_la_purga_falla_registra_el_error_y_el_worker_no_muere()
    {
        var repositorio = new RepositorioFalso { Error = new InvalidOperationException("MySQL no responde") };
        var logger = new LoggerQueEspera<RetencionTelemetriaWorker>(LogLevel.Error);
        using var worker = CrearWorker(repositorio, new ColectorOptions(), logger);

        await worker.StartAsync(CancellationToken.None);
        await logger.Esperado.WaitAsync(EsperaMaxima);

        Assert.False(worker.ExecuteTask!.IsCompleted);
        await worker.StopAsync(CancellationToken.None);
        Assert.True(worker.ExecuteTask.IsCompleted);
    }

    [Fact]
    public async Task Si_se_cancela_mientras_purga_el_worker_termina_sin_registrar_error()
    {
        var repositorio = new RepositorioFalso { BloquearHastaCancelar = true };
        var logger = new LoggerQueEspera<RetencionTelemetriaWorker>(LogLevel.Error);
        using var worker = CrearWorker(repositorio, new ColectorOptions(), logger);

        await worker.StartAsync(CancellationToken.None);
        await repositorio.PrimeraLlamada.WaitAsync(EsperaMaxima);
        await worker.StopAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(EsperaMaxima);

        Assert.False(logger.Esperado.IsCompleted);
    }

    private static RetencionTelemetriaWorker CrearWorker(RepositorioFalso repositorio, ColectorOptions opciones,
        ILogger<RetencionTelemetriaWorker>? logger = null)
    {
        // Un contenedor real, igual que en Program.cs: el worker pide un scope por pasada y el servicio
        // llega construido con sus dependencias.
        var servicios = new ServiceCollection();
        servicios.AddSingleton<ITelemetriaRepository>(repositorio);
        servicios.AddSingleton<TimeProvider>(new RelojFijo(Ahora));
        servicios.AddScoped<RetencionTelemetriaService>();
        var proveedor = servicios.BuildServiceProvider();

        return new RetencionTelemetriaWorker(
            proveedor.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(opciones),
            logger ?? NullLogger<RetencionTelemetriaWorker>.Instance);
    }

    private sealed class RelojFijo(DateTimeOffset momento) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => momento;
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

    /// <summary>
    /// Repositorio falso: solo importa el borrado. Lo demás falla si alguien lo llama por error.
    /// </summary>
    private sealed class RepositorioFalso : ITelemetriaRepository
    {
        private readonly TaskCompletionSource _primeraLlamada = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _llamadas;

        public int Borradas { get; init; }
        public Exception? Error { get; init; }
        public bool BloquearHastaCancelar { get; init; }
        public DateTime? UltimoLimite { get; private set; }
        public int Llamadas => Volatile.Read(ref _llamadas);

        public Task PrimeraLlamada => _primeraLlamada.Task;

        public async Task<int> EliminarAnterioresAsync(DateTime limiteUtc, CancellationToken cancellationToken = default)
        {
            UltimoLimite = limiteUtc;
            Interlocked.Increment(ref _llamadas);
            _primeraLlamada.TrySetResult();

            if (BloquearHastaCancelar)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            if (Error is not null)
            {
                throw Error;
            }

            return Borradas;
        }

        public Task AgregarAsync(LecturaTelemetria lectura, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AgregarVariasAsync(IReadOnlyList<LecturaTelemetria> lecturas,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<LecturaTelemetria>> ObtenerPorPanelAsync(Guid panelSolarId, DateTime desde,
            DateTime hasta, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<LecturaTelemetria>> ObtenerUltimasPorPanelAsync(Guid panelSolarId, int cantidad,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<AgregadoPotencia>> ObtenerAgregadoPorPanelAsync(Guid panelSolarId,
            GranularidadConsulta granularidad, DateTime? desde = null, DateTime? hasta = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<AgregadoPotencia>> ObtenerAgregadoGeneralAsync(Guid? parqueSolarId,
            GranularidadConsulta granularidad, DateTime? desde = null, DateTime? hasta = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<LecturaTelemetria>> ObtenerDesdeAsync(DateTime desdeUtc,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
