using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolarUY.Application.Services;
using SolarUY.Collector;
using SolarUY.Collector.Configuration;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Collector.Tests;

/// <summary>
/// Pruebas de lo que el worker de retención deja en el log. Los mensajes de nivel Information solo se
/// arman cuando ese nivel está habilitado (para evitar el costo de los argumentos), así que acá se usa un
/// logger que lo habilita y guarda el texto.
/// </summary>
public class RetencionTelemetriaWorkerRegistroTests
{
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Al_arrancar_informa_cuantos_dias_conserva_y_cada_cuantas_horas_corre()
    {
        var logger = new LoggerQueAnota<RetencionTelemetriaWorker>();
        using var worker = CrearWorker(new RepositorioQueBorra(0), new ColectorOptions
        {
            RetencionDias = 45,
            IntervaloRetencionHoras = 12
        }, logger);

        await worker.StartAsync(CancellationToken.None);
        await logger.EsperarAsync(texto => texto.Contains("iniciada"), EsperaMaxima);
        await worker.StopAsync(CancellationToken.None);

        var aviso = Assert.Single(logger.Mensajes, mensaje => mensaje.Contains("iniciada"));
        Assert.Contains("45", aviso);
        Assert.Contains("12", aviso);
    }

    [Fact]
    public async Task Cuando_borra_filas_informa_cuantas_fueron()
    {
        var logger = new LoggerQueAnota<RetencionTelemetriaWorker>();
        using var worker = CrearWorker(new RepositorioQueBorra(7), new ColectorOptions { RetencionDias = 30 }, logger);

        await worker.StartAsync(CancellationToken.None);
        await logger.EsperarAsync(texto => texto.Contains("filas borradas"), EsperaMaxima);
        await worker.StopAsync(CancellationToken.None);

        var aviso = Assert.Single(logger.Mensajes, mensaje => mensaje.Contains("filas borradas"));
        Assert.Contains("7", aviso);
        Assert.Contains("30", aviso);
    }

    private static RetencionTelemetriaWorker CrearWorker(RepositorioQueBorra repositorio, ColectorOptions opciones,
        ILogger<RetencionTelemetriaWorker> logger)
    {
        var servicios = new ServiceCollection();
        servicios.AddSingleton<ITelemetriaRepository>(repositorio);
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddScoped<RetencionTelemetriaService>();
        var proveedor = servicios.BuildServiceProvider();

        return new RetencionTelemetriaWorker(proveedor.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(opciones), logger);
    }

    /// <summary>Logger con todos los niveles habilitados que guarda el texto ya armado de cada mensaje.</summary>
    private sealed class LoggerQueAnota<T> : ILogger<T>
    {
        private readonly List<string> _mensajes = [];

        public IReadOnlyList<string> Mensajes
        {
            get
            {
                lock (_mensajes)
                {
                    return _mensajes.ToList();
                }
            }
        }

        public async Task EsperarAsync(Func<string, bool> condicion, TimeSpan tope)
        {
            var limite = DateTime.UtcNow + tope;
            while (!Mensajes.Any(condicion))
            {
                if (DateTime.UtcNow > limite)
                {
                    throw new TimeoutException("El mensaje esperado nunca se registró.");
                }

                await Task.Delay(20);
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (_mensajes)
            {
                _mensajes.Add(formatter(state, exception));
            }
        }
    }

    /// <summary>Repositorio que informa siempre la misma cantidad de filas borradas; lo demás falla si se llama.</summary>
    private sealed class RepositorioQueBorra(int borradas) : ITelemetriaRepository
    {
        public Task<int> EliminarAnterioresAsync(DateTime limiteUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(borradas);

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
