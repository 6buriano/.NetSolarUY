using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="ReporteService"/>: etiquetas de cada período, estimación de energía,
/// validación del rango de fechas y el cálculo de "hoy" en hora de Montevideo.
/// </summary>
public class ReporteServiceTests
{
    // Con el colector cada 5 s, una suma de 720.000 W equivale a 720000 × 5 / 3600 = 1000 Wh = 1 kWh.
    private const double SumaQueEquivaleAUnKWh = 720_000;

    private static ReporteService CrearServicio(TelemetriaRepositoryEnMemoria telemetria,
        ParqueRepositoryEnMemoria? parques = null, DateTimeOffset? ahora = null)
        => new(telemetria, parques ?? new ParqueRepositoryEnMemoria(),
            new RelojFijo(ahora ?? new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero)));

    private static ParqueSolar ParqueDePrueba()
        => new("Parque Norte", "Ruta 5 km 20", 100, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

    // ---------------------------------------------------------------- Por panel

    [Fact]
    public async Task ObtenerGeneracionPorPanelAsync_RedondeaLosValoresYEstimaLaEnergia()
    {
        var telemetria = new TelemetriaRepositoryEnMemoria
        {
            Agregados =
            [
                new AgregadoPotencia(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), 100, 123.456, 200.789,
                    SumaQueEquivaleAUnKWh)
            ]
        };
        var panelId = Guid.NewGuid();

        var resultado = await CrearServicio(telemetria)
            .ObtenerGeneracionPorPanelAsync(panelId, GranularidadConsulta.DIA);

        var fila = Assert.Single(resultado);
        Assert.Equal(100, fila.CantidadLecturas);
        Assert.Equal(123.46, fila.PotenciaPromedioW);
        Assert.Equal(200.79, fila.PotenciaMaximaW);
        Assert.Equal(1.0, fila.EnergiaEstimadaKWh);
        Assert.Equal(panelId, telemetria.UltimoPanelId);
    }

    [Theory]
    [InlineData(GranularidadConsulta.MES, "noviembre 2026")]
    [InlineData(GranularidadConsulta.ANIO, "2026")]
    public async Task ObtenerGeneracionPorPanelAsync_EtiquetaElPeriodoSegunLaGranularidad(
        GranularidadConsulta granularidad, string etiquetaEsperada)
    {
        var telemetria = new TelemetriaRepositoryEnMemoria
        {
            Agregados = [new AgregadoPotencia(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), 1, 1, 1, 1)]
        };

        var resultado = await CrearServicio(telemetria)
            .ObtenerGeneracionPorPanelAsync(Guid.NewGuid(), granularidad);

        Assert.Equal(etiquetaEsperada, Assert.Single(resultado).Etiqueta);
        Assert.Equal(granularidad, telemetria.UltimaGranularidad);
    }

    [Fact]
    public async Task ObtenerGeneracionPorPanelAsync_PasaElRangoDeFechasAlRepositorio()
    {
        var telemetria = new TelemetriaRepositoryEnMemoria();
        var desde = new DateTime(2026, 11, 1);
        var hasta = new DateTime(2026, 11, 30);

        await CrearServicio(telemetria)
            .ObtenerGeneracionPorPanelAsync(Guid.NewGuid(), GranularidadConsulta.DIA, desde, hasta);

        Assert.Equal(desde, telemetria.UltimoDesde);
        Assert.Equal(hasta, telemetria.UltimoHasta);
    }

    // ---------------------------------------------------------------- Por parque

    [Fact]
    public async Task ObtenerGeneracionAsync_SiElParqueNoExiste_LanzaExcepcion()
    {
        var servicio = CrearServicio(new TelemetriaRepositoryEnMemoria());

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.ObtenerGeneracionAsync(Guid.NewGuid(), GranularidadConsulta.DIA, null, null));

        Assert.Equal("El parque indicado no existe.", error.Message);
    }

    [Fact]
    public async Task ObtenerGeneracionAsync_ConDesdePosteriorAHasta_LanzaExcepcionYNoConsultaLaTelemetria()
    {
        var parque = ParqueDePrueba();
        var telemetria = new TelemetriaRepositoryEnMemoria();
        var servicio = CrearServicio(telemetria, new ParqueRepositoryEnMemoria(parque));

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => servicio.ObtenerGeneracionAsync(
            parque.Id, GranularidadConsulta.DIA, new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 1)));

        Assert.Equal("La fecha desde no puede ser posterior a la fecha hasta.", error.Message);
        Assert.Null(telemetria.UltimaGranularidad);
    }

    [Fact]
    public async Task ObtenerGeneracionAsync_IncluyeElDiaHastaCompletoYEstimaLaEnergia()
    {
        var parque = ParqueDePrueba();
        var telemetria = new TelemetriaRepositoryEnMemoria
        {
            Agregados =
            [
                new AgregadoPotencia(new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc), 50, 10, 20,
                    SumaQueEquivaleAUnKWh / 2)
            ]
        };
        var servicio = CrearServicio(telemetria, new ParqueRepositoryEnMemoria(parque));

        var resultado = await servicio.ObtenerGeneracionAsync(parque.Id, GranularidadConsulta.MES,
            new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        var fila = Assert.Single(resultado);
        Assert.Equal("noviembre 2026", fila.Etiqueta);
        Assert.Equal(50, fila.CantidadLecturas);
        Assert.Equal(0.5, fila.EnergiaEstimadaKWh);
        Assert.Equal(parque.Id, telemetria.UltimoParqueId);
        Assert.Equal(new DateTime(2026, 11, 1, 0, 0, 0), telemetria.UltimoDesde);
        Assert.Equal(new DateOnly(2026, 11, 30).ToDateTime(TimeOnly.MaxValue), telemetria.UltimoHasta);
    }

    [Fact]
    public async Task ObtenerGeneracionAsync_SinFechas_NoLimitaElRango()
    {
        var parque = ParqueDePrueba();
        var telemetria = new TelemetriaRepositoryEnMemoria();
        var servicio = CrearServicio(telemetria, new ParqueRepositoryEnMemoria(parque));

        var resultado = await servicio.ObtenerGeneracionAsync(parque.Id, GranularidadConsulta.ANIO, null, null);

        Assert.Empty(resultado);
        Assert.Null(telemetria.UltimoDesde);
        Assert.Null(telemetria.UltimoHasta);
    }

    // ---------------------------------------------------------------- Hoy

    [Fact]
    public async Task ObtenerGeneracionHoyAsync_UsaElDiaCalendarioDeMontevideoYSumaLosPeriodos()
    {
        // 01:00 UTC del 12/11 son las 22:00 del 11/11 en Montevideo (UTC−3): "hoy" empezó a las
        // 00:00 del 11/11 hora local, que son las 03:00 UTC.
        var ahora = new DateTimeOffset(2026, 11, 12, 1, 0, 0, TimeSpan.Zero);
        var telemetria = new TelemetriaRepositoryEnMemoria
        {
            Agregados =
            [
                new AgregadoPotencia(new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc), 10, 1, 1,
                    SumaQueEquivaleAUnKWh / 2),
                new AgregadoPotencia(new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc), 20, 1, 1,
                    SumaQueEquivaleAUnKWh / 2)
            ]
        };

        var resultado = await CrearServicio(telemetria, ahora: ahora).ObtenerGeneracionHoyAsync();

        Assert.Equal("hoy", resultado.Etiqueta);
        Assert.Equal(30, resultado.CantidadLecturas);
        Assert.Equal(1.0, resultado.EnergiaEstimadaKWh);
        Assert.Null(telemetria.UltimoParqueId);
        Assert.Equal(GranularidadConsulta.DIA, telemetria.UltimaGranularidad);
        Assert.Equal(new DateTime(2026, 11, 11, 3, 0, 0), telemetria.UltimoDesde);
        Assert.Equal(new DateTime(2026, 11, 12, 1, 0, 0), telemetria.UltimoHasta);
    }

    [Fact]
    public async Task ObtenerGeneracionHoyAsync_SinLecturas_DevuelveCero()
    {
        var resultado = await CrearServicio(new TelemetriaRepositoryEnMemoria()).ObtenerGeneracionHoyAsync();

        Assert.Equal("hoy", resultado.Etiqueta);
        Assert.Equal(0, resultado.CantidadLecturas);
        Assert.Equal(0, resultado.EnergiaEstimadaKWh);
    }

    /// <summary>Reloj congelado: el "hoy" de Montevideo se prueba sin depender de la hora de la máquina.</summary>
    private sealed class RelojFijo(DateTimeOffset momento) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => momento;
    }
}
