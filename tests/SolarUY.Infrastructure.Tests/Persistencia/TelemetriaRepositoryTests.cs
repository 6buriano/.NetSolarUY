using SolarUY.Domain.Enums;
using SolarUY.Infrastructure.Repositories;

namespace SolarUY.Infrastructure.Tests.Persistencia;

/// <summary>
/// Pruebas de <see cref="TelemetriaRepository"/> contra SQLite en memoria: filtros por fecha, orden, borrado
/// por retención y las agregaciones por día, mes y año que arma el SQL de EF.
/// </summary>
public class TelemetriaRepositoryTests
{
    private static readonly DateTime D10Mediodia = new(2026, 11, 10, 12, 0, 0);
    private static readonly DateTime D10Manana = new(2026, 11, 10, 10, 0, 0);
    private static readonly DateTime D11 = new(2026, 11, 11, 10, 0, 0);
    private static readonly DateTime Octubre = new(2026, 10, 5, 9, 0, 0);
    private static readonly DateTime AnioAnterior = new(2025, 12, 31, 23, 0, 0);

    /// <summary>Cinco lecturas de un mismo panel repartidas en cuatro días, tres meses y dos años.</summary>
    private static async Task<Guid> SembrarLecturasAsync(BaseDeDatosEnMemoria db)
    {
        var (_, _, panel) = await Datos.SembrarAsync(db);
        await db.GuardarAsync(
            Datos.NuevaLectura(panel.Id, D10Manana, 100d),
            Datos.NuevaLectura(panel.Id, D10Mediodia, 300d),
            Datos.NuevaLectura(panel.Id, D11, 200d),
            Datos.NuevaLectura(panel.Id, Octubre, 50d),
            Datos.NuevaLectura(panel.Id, AnioAnterior, 10d));
        return panel.Id;
    }

    // ---------------------------------------------------------------- Escritura

    [Fact]
    public async Task Agregar_GuardaLaLectura()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, _, panel) = await Datos.SembrarAsync(db);
        var lectura = Datos.NuevaLectura(panel.Id, D10Manana, 321d);

        await using (var contexto = db.NuevoContexto())
        {
            await new TelemetriaRepository(contexto).AgregarAsync(lectura);
        }

        await using var lectorContexto = db.NuevoContexto();
        var guardada = Assert.Single(lectorContexto.LecturasTelemetria);
        Assert.Equal(lectura.Id, guardada.Id);
        Assert.Equal(321d, guardada.PotenciaGeneradaWatts, 6);
        Assert.Equal(D10Manana, guardada.FechaHora);
        Assert.Equal(EstadoSalud.OPERATIVO, guardada.EstadoDetectado);
    }

    [Fact]
    public async Task AgregarVarias_GuardaTodasLasLecturasEnUnaSolaOperacion()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, _, panel) = await Datos.SembrarAsync(db);

        await using (var contexto = db.NuevoContexto())
        {
            await new TelemetriaRepository(contexto).AgregarVariasAsync(
            [
                Datos.NuevaLectura(panel.Id, D10Manana),
                Datos.NuevaLectura(panel.Id, D10Mediodia),
                Datos.NuevaLectura(panel.Id, D11)
            ]);
        }

        await using var lectura = db.NuevoContexto();
        Assert.Equal(3, lectura.LecturasTelemetria.Count());
    }

    [Fact]
    public async Task AgregarVarias_ConUnaListaVacia_NoHaceNada()
    {
        using var db = new BaseDeDatosEnMemoria();
        await using var contexto = db.NuevoContexto();

        await new TelemetriaRepository(contexto).AgregarVariasAsync([]);

        Assert.Empty(contexto.LecturasTelemetria);
    }

    // ---------------------------------------------------------------- Consultas de lecturas

    [Fact]
    public async Task ObtenerPorPanel_DevuelveLasLecturasDelRangoDeLaMasNuevaALaMasVieja()
    {
        using var db = new BaseDeDatosEnMemoria();
        var panelId = await SembrarLecturasAsync(db);

        await using var contexto = db.NuevoContexto();
        var lecturas = await new TelemetriaRepository(contexto).ObtenerPorPanelAsync(panelId, D10Manana, D11);

        Assert.Equal(new[] { D11, D10Mediodia, D10Manana }, lecturas.Select(lectura => lectura.FechaHora).ToArray());
    }

    [Fact]
    public async Task ObtenerPorPanel_NoMezclaLecturasDeOtrosPaneles()
    {
        using var db = new BaseDeDatosEnMemoria();
        var panelId = await SembrarLecturasAsync(db);
        var (_, _, otro) = await Datos.SembrarAsync(db, "B");
        await db.GuardarAsync(Datos.NuevaLectura(otro.Id, D10Mediodia));

        await using var contexto = db.NuevoContexto();
        var lecturas = await new TelemetriaRepository(contexto).ObtenerPorPanelAsync(panelId, AnioAnterior, D11);

        Assert.Equal(5, lecturas.Count);
        Assert.All(lecturas, lectura => Assert.Equal(panelId, lectura.PanelSolarId));
    }

    [Fact]
    public async Task ObtenerUltimasPorPanel_DevuelveLasMasRecientesPrimero()
    {
        using var db = new BaseDeDatosEnMemoria();
        var panelId = await SembrarLecturasAsync(db);

        await using var contexto = db.NuevoContexto();
        var lecturas = await new TelemetriaRepository(contexto).ObtenerUltimasPorPanelAsync(panelId, 2);

        Assert.Equal(new[] { D11, D10Mediodia }, lecturas.Select(lectura => lectura.FechaHora).ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task ObtenerUltimasPorPanel_ConUnaCantidadNoPositiva_DevuelveUnaListaVacia(int cantidad)
    {
        using var db = new BaseDeDatosEnMemoria();
        var panelId = await SembrarLecturasAsync(db);

        await using var contexto = db.NuevoContexto();

        Assert.Empty(await new TelemetriaRepository(contexto).ObtenerUltimasPorPanelAsync(panelId, cantidad));
    }

    [Fact]
    public async Task ObtenerDesde_DevuelveLasLecturasDesdeEsaFechaEnAdelante()
    {
        using var db = new BaseDeDatosEnMemoria();
        await SembrarLecturasAsync(db);

        await using var contexto = db.NuevoContexto();
        var lecturas = await new TelemetriaRepository(contexto).ObtenerDesdeAsync(D10Mediodia);

        Assert.Equal(2, lecturas.Count);
        Assert.All(lecturas, lectura => Assert.True(lectura.FechaHora >= D10Mediodia));
    }

    [Fact]
    public async Task EliminarAnteriores_BorraLasMasViejasQueElLimiteYDevuelveCuantas()
    {
        using var db = new BaseDeDatosEnMemoria();
        await SembrarLecturasAsync(db);

        int borradas;
        await using (var contexto = db.NuevoContexto())
        {
            borradas = await new TelemetriaRepository(contexto).EliminarAnterioresAsync(D10Mediodia);
        }

        await using var lectura = db.NuevoContexto();
        Assert.Equal(3, borradas);
        Assert.Equal(2, lectura.LecturasTelemetria.Count());
        Assert.All(lectura.LecturasTelemetria, l => Assert.True(l.FechaHora >= D10Mediodia));
    }

    [Fact]
    public async Task EliminarAnteriores_SinLecturasViejas_DevuelveCero()
    {
        using var db = new BaseDeDatosEnMemoria();
        await SembrarLecturasAsync(db);

        await using var contexto = db.NuevoContexto();

        Assert.Equal(0, await new TelemetriaRepository(contexto).EliminarAnterioresAsync(new DateTime(2000, 1, 1)));
    }

    // ---------------------------------------------------------------- Agregados por panel

    [Fact]
    public async Task AgregadoPorPanel_PorDia_AgrupaYOrdenaDelDiaMasNuevoAlMasViejo()
    {
        using var db = new BaseDeDatosEnMemoria();
        var panelId = await SembrarLecturasAsync(db);

        await using var contexto = db.NuevoContexto();
        var dias = await new TelemetriaRepository(contexto).ObtenerAgregadoPorPanelAsync(panelId, GranularidadConsulta.DIA);

        Assert.Equal(
            new[] { new DateTime(2026, 11, 11), new DateTime(2026, 11, 10), new DateTime(2026, 10, 5), new DateTime(2025, 12, 31) },
            dias.Select(dia => dia.PeriodoInicio).ToArray());

        var dia10 = dias[1];
        Assert.Equal(2, dia10.CantidadLecturas);
        Assert.Equal(200d, dia10.PotenciaPromedioW, 6);
        Assert.Equal(300d, dia10.PotenciaMaximaW, 6);
        Assert.Equal(400d, dia10.PotenciaSumaW, 6);
    }

    [Fact]
    public async Task AgregadoPorPanel_PorMes_AgrupaPorMes()
    {
        using var db = new BaseDeDatosEnMemoria();
        var panelId = await SembrarLecturasAsync(db);

        await using var contexto = db.NuevoContexto();
        var meses = await new TelemetriaRepository(contexto).ObtenerAgregadoPorPanelAsync(panelId, GranularidadConsulta.MES);

        Assert.Equal(
            new[] { new DateTime(2026, 11, 1), new DateTime(2026, 10, 1), new DateTime(2025, 12, 1) },
            meses.Select(mes => mes.PeriodoInicio).ToArray());
        Assert.Equal(3, meses[0].CantidadLecturas);
        Assert.Equal(600d, meses[0].PotenciaSumaW, 6);
        Assert.Equal(200d, meses[0].PotenciaPromedioW, 6);
        Assert.Equal(300d, meses[0].PotenciaMaximaW, 6);
    }

    [Fact]
    public async Task AgregadoPorPanel_PorAnio_AgrupaPorAnio()
    {
        using var db = new BaseDeDatosEnMemoria();
        var panelId = await SembrarLecturasAsync(db);

        await using var contexto = db.NuevoContexto();
        var anios = await new TelemetriaRepository(contexto).ObtenerAgregadoPorPanelAsync(panelId, GranularidadConsulta.ANIO);

        Assert.Equal(new[] { new DateTime(2026, 1, 1), new DateTime(2025, 1, 1) },
            anios.Select(anio => anio.PeriodoInicio).ToArray());
        Assert.Equal(4, anios[0].CantidadLecturas);
        Assert.Equal(650d, anios[0].PotenciaSumaW, 6);
        Assert.Equal(162.5, anios[0].PotenciaPromedioW, 6);
    }

    [Fact]
    public async Task AgregadoPorPanel_ConDesdeYHasta_SoloCuentaEseRango()
    {
        using var db = new BaseDeDatosEnMemoria();
        var panelId = await SembrarLecturasAsync(db);

        await using var contexto = db.NuevoContexto();
        var dias = await new TelemetriaRepository(contexto).ObtenerAgregadoPorPanelAsync(
            panelId, GranularidadConsulta.DIA, new DateTime(2026, 11, 10), new DateTime(2026, 11, 10, 23, 59, 59));

        var dia = Assert.Single(dias);
        Assert.Equal(new DateTime(2026, 11, 10), dia.PeriodoInicio);
        Assert.Equal(2, dia.CantidadLecturas);
    }

    [Fact]
    public async Task AgregadoPorPanel_SinLecturas_DevuelveUnaListaVacia()
    {
        using var db = new BaseDeDatosEnMemoria();
        await using var contexto = db.NuevoContexto();

        var dias = await new TelemetriaRepository(contexto).ObtenerAgregadoPorPanelAsync(
            Guid.NewGuid(), GranularidadConsulta.DIA);

        Assert.Empty(dias);
    }

    // ---------------------------------------------------------------- Agregado general

    [Fact]
    public async Task AgregadoGeneral_SinParque_SumaLasLecturasDeTodosLosPaneles()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, _, panelA) = await Datos.SembrarAsync(db, "A");
        var (_, _, panelB) = await Datos.SembrarAsync(db, "B");
        await db.GuardarAsync(
            Datos.NuevaLectura(panelA.Id, D10Manana, 100d),
            Datos.NuevaLectura(panelB.Id, D10Mediodia, 300d));

        await using var contexto = db.NuevoContexto();
        var dias = await new TelemetriaRepository(contexto).ObtenerAgregadoGeneralAsync(null, GranularidadConsulta.DIA);

        var dia = Assert.Single(dias);
        Assert.Equal(2, dia.CantidadLecturas);
        Assert.Equal(400d, dia.PotenciaSumaW, 6);
    }

    [Fact]
    public async Task AgregadoGeneral_ConParque_SoloCuentaLosPanelesDeEseParque()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (parqueA, _, panelA) = await Datos.SembrarAsync(db, "A");
        var (_, _, panelB) = await Datos.SembrarAsync(db, "B");
        await db.GuardarAsync(
            Datos.NuevaLectura(panelA.Id, D10Manana, 100d),
            Datos.NuevaLectura(panelB.Id, D10Mediodia, 300d));

        await using var contexto = db.NuevoContexto();
        var dias = await new TelemetriaRepository(contexto).ObtenerAgregadoGeneralAsync(
            parqueA.Id, GranularidadConsulta.DIA);

        var dia = Assert.Single(dias);
        Assert.Equal(1, dia.CantidadLecturas);
        Assert.Equal(100d, dia.PotenciaSumaW, 6);
    }

    [Fact]
    public async Task AgregadoGeneral_ConDesdeYHasta_SoloCuentaEseRango()
    {
        using var db = new BaseDeDatosEnMemoria();
        await SembrarLecturasAsync(db);

        await using var contexto = db.NuevoContexto();
        var meses = await new TelemetriaRepository(contexto).ObtenerAgregadoGeneralAsync(
            null, GranularidadConsulta.MES, new DateTime(2026, 11, 1), new DateTime(2026, 11, 30));

        var mes = Assert.Single(meses);
        Assert.Equal(new DateTime(2026, 11, 1), mes.PeriodoInicio);
        Assert.Equal(3, mes.CantidadLecturas);
    }
}
