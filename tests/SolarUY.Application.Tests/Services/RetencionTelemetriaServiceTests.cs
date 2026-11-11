using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Tests.Services;

public class RetencionTelemetriaServiceTests
{
    [Fact]
    public async Task PurgarAsync_BorraHastaElLimiteCalculadoYDevuelveLaCantidad()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 11, 11, 15, 0, 0, TimeSpan.Zero));
        var repositorio = new TelemetriaRepositoryFalso { FilasBorradas = 42 };
        var servicio = new RetencionTelemetriaService(repositorio, reloj);

        var borradas = await servicio.PurgarAsync(90);

        Assert.Equal(42, borradas);
        Assert.Equal(new DateTime(2026, 8, 13, 15, 0, 0, DateTimeKind.Utc), repositorio.UltimoLimite);
        Assert.Equal(1, repositorio.LlamadasAEliminar);
    }

    [Fact]
    public async Task PurgarAsync_CuandoNoHayLecturasViejas_DevuelveCero()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 11, 11, 15, 0, 0, TimeSpan.Zero));
        var repositorio = new TelemetriaRepositoryFalso { FilasBorradas = 0 };
        var servicio = new RetencionTelemetriaService(repositorio, reloj);

        var borradas = await servicio.PurgarAsync(RetencionTelemetriaService.DiasPorDefecto);

        Assert.Equal(0, borradas);
        Assert.Equal(1, repositorio.LlamadasAEliminar);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PurgarAsync_ConDiasNoPositivos_LanzaExcepcionYNoTocaLaBase(int dias)
    {
        var repositorio = new TelemetriaRepositoryFalso();
        var servicio = new RetencionTelemetriaService(repositorio,
            new RelojFijo(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => servicio.PurgarAsync(dias));
        Assert.Equal(0, repositorio.LlamadasAEliminar);
    }

    [Fact]
    public void CalcularLimite_RestaLosDiasAlRelojYDevuelveUnaFechaUtc()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 2, 1, 3, 30, 0, TimeSpan.Zero));
        var servicio = new RetencionTelemetriaService(new TelemetriaRepositoryFalso(), reloj);

        var limite = servicio.CalcularLimite(30);

        Assert.Equal(new DateTime(2026, 1, 2, 3, 30, 0, DateTimeKind.Utc), limite);
        Assert.Equal(DateTimeKind.Utc, limite.Kind);
    }

    [Fact]
    public void CalcularLimite_ArrancaEnElValorPorDefectoDelProyecto()
        => Assert.Equal(90, RetencionTelemetriaService.DiasPorDefecto);

    /// <summary>Reloj congelado: la política se prueba sin depender de la hora de la máquina.</summary>
    private sealed class RelojFijo(DateTimeOffset momento) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => momento;
    }

    /// <summary>
    /// Repositorio falso: sólo importa el borrado. El resto de las operaciones no se usan en estas
    /// pruebas, así que fallan si alguien las llama por error.
    /// </summary>
    private sealed class TelemetriaRepositoryFalso : ITelemetriaRepository
    {
        public int FilasBorradas { get; init; }
        public int LlamadasAEliminar { get; private set; }
        public DateTime? UltimoLimite { get; private set; }

        public Task<int> EliminarAnterioresAsync(DateTime limiteUtc, CancellationToken cancellationToken = default)
        {
            LlamadasAEliminar++;
            UltimoLimite = limiteUtc;
            return Task.FromResult(FilasBorradas);
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
