using System.Text.Json;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;
using SolarUY.Infrastructure.Mcp.Tools;

namespace SolarUY.Infrastructure.Tests;

/// <summary>
/// Pruebas de las herramientas MCP que usa el agente. Se llama a cada método directamente, sin
/// servidor MCP ni red: lo que se verifica es el texto o el JSON que le llega al modelo.
/// </summary>
public class HerramientasMcpTests
{
    // Con el colector cada 5 s, una suma de 720.000 W equivale a 1 kWh.
    private const double SumaQueEquivaleAUnKWh = 720_000;

    private static readonly DateTimeOffset Ahora = new(2026, 11, 12, 15, 0, 0, TimeSpan.Zero);

    private static AgregadoPotencia Agregado(DateTime inicio, int lecturas = 10)
        => new(inicio, lecturas, 100, 200, SumaQueEquivaleAUnKWh);

    private static ReporteService CrearReporte(TelemetriaRepositoryFalso telemetria, params ParqueSolar[] parques)
        => new(telemetria, new ParqueRepositoryFalso(parques), new RelojFijo(Ahora));

    private static PanelService CrearPanelService(IReadOnlyList<PanelSolar> paneles)
        => new(new PanelRepositoryFalso(paneles), new NodoRepositoryFalso(), new TelemetriaRepositoryFalso());

    // ---------------------------------------------------------------- listar_parques

    [Fact]
    public async Task ListarParques_DevuelveElJsonConElIdYLosTotalesDeCadaParque()
    {
        var parque = Fixture.Parque();
        var nodo = Fixture.Nodo(parque);
        Fixture.Panel(nodo, "P01");
        Fixture.Panel(nodo, "P02");
        var herramienta = new ListarParquesTool(new ParqueService(new ParqueRepositoryFalso(parque)));

        var json = await herramienta.ListarAsync();

        using var documento = JsonDocument.Parse(json);
        var elemento = Assert.Single(documento.RootElement.EnumerateArray());
        Assert.Equal(parque.Id, elemento.GetProperty("id").GetGuid());
        Assert.Equal("Parque Norte", elemento.GetProperty("nombre").GetString());
        Assert.Equal(1, elemento.GetProperty("cantidadNodos").GetInt32());
        Assert.Equal(2, elemento.GetProperty("cantidadPaneles").GetInt32());
        Assert.Equal(0.6, elemento.GetProperty("potenciaNominalPanelesKW").GetDouble(), 3);
    }

    [Fact]
    public async Task ListarParques_SinParques_DevuelveUnaListaVacia()
    {
        var herramienta = new ListarParquesTool(new ParqueService(new ParqueRepositoryFalso()));

        var json = await herramienta.ListarAsync();

        Assert.Equal("[]", json);
    }

    // ---------------------------------------------------------------- obtener_resumen_parque

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-guid")]
    public async Task ObtenerResumenParque_ConUnIdInvalido_ExplicaQueElIdNoEsValido(string parqueId)
    {
        var herramienta = new ObtenerResumenParqueTool(new ParqueService(new ParqueRepositoryFalso()));

        var texto = await herramienta.ObtenerAsync(parqueId);

        Assert.Contains("identificador del parque no es válido", texto);
    }

    [Fact]
    public async Task ObtenerResumenParque_SiElParqueNoExiste_DevuelveElMensajeDeLaRegla()
    {
        var herramienta = new ObtenerResumenParqueTool(new ParqueService(new ParqueRepositoryFalso()));

        var texto = await herramienta.ObtenerAsync(Guid.NewGuid().ToString());

        Assert.Equal("El parque indicado no existe.", texto);
    }

    [Fact]
    public async Task ObtenerResumenParque_DevuelveLosConteosPorEstado()
    {
        var parque = Fixture.Parque();
        var nodo = Fixture.Nodo(parque);
        Fixture.Panel(nodo, "P01", EstadoSalud.OPERATIVO);
        Fixture.Panel(nodo, "P02", EstadoSalud.OPERATIVO);
        Fixture.Panel(nodo, "P03", EstadoSalud.RENDIMIENTO_BAJO);
        Fixture.Panel(nodo, "P04", EstadoSalud.FALLA_DETECTADA);
        Fixture.Panel(nodo, "P05", EstadoSalud.DESCONECTADO);
        var herramienta = new ObtenerResumenParqueTool(new ParqueService(new ParqueRepositoryFalso(parque)));

        var json = await herramienta.ObtenerAsync(parque.Id.ToString());

        using var documento = JsonDocument.Parse(json);
        var raiz = documento.RootElement;
        Assert.Equal("Parque Norte", raiz.GetProperty("nombre").GetString());
        Assert.Equal(1, raiz.GetProperty("cantidadNodos").GetInt32());
        Assert.Equal(5, raiz.GetProperty("cantidadPaneles").GetInt32());
        Assert.Equal(2, raiz.GetProperty("panelesOperativos").GetInt32());
        Assert.Equal(1, raiz.GetProperty("panelesRendimientoBajo").GetInt32());
        Assert.Equal(1, raiz.GetProperty("panelesEnFalla").GetInt32());
        Assert.Equal(1, raiz.GetProperty("panelesDesconectados").GetInt32());
    }

    // ---------------------------------------------------------------- listar_paneles_en_falla

    [Fact]
    public async Task ListarPanelesEnFalla_SiNoHayNinguno_DevuelveUnTexto()
    {
        var parque = Fixture.Parque();
        var nodo = Fixture.Nodo(parque);
        var herramienta = new ListarPanelesEnFallaTool(CrearPanelService([Fixture.Panel(nodo, "P01")]));

        var texto = await herramienta.ObtenerAsync();

        Assert.Contains("ningún panel está en falla", texto);
    }

    [Fact]
    public async Task ListarPanelesEnFalla_DevuelveSoloLosQueEstanEnFalla()
    {
        var nodo = Fixture.Nodo(Fixture.Parque());
        var enFalla = Fixture.Panel(nodo, "P01", EstadoSalud.FALLA_DETECTADA);
        var desconectado = Fixture.Panel(nodo, "P02", EstadoSalud.DESCONECTADO);
        var operativo = Fixture.Panel(nodo, "P03");
        var herramienta = new ListarPanelesEnFallaTool(CrearPanelService([enFalla, desconectado, operativo]));

        var json = await herramienta.ObtenerAsync();

        using var documento = JsonDocument.Parse(json);
        var raiz = documento.RootElement;
        Assert.Equal(1, raiz.GetProperty("cantidadEnFalla").GetInt32());
        var panel = Assert.Single(raiz.GetProperty("paneles").EnumerateArray());
        Assert.Equal("P01", panel.GetProperty("codigoPanel").GetString());
        Assert.Equal("FALLA_DETECTADA", panel.GetProperty("estado").GetString());
        Assert.False(raiz.TryGetProperty("aviso", out _));
    }

    [Fact]
    public async Task ListarPanelesEnFalla_ConMasDeDoscientos_RecortaLaListaPeroInformaElTotal()
    {
        var nodo = Fixture.Nodo(Fixture.Parque());
        var paneles = Enumerable.Range(1, 201)
            .Select(numero => Fixture.Panel(nodo, $"P{numero:000}", EstadoSalud.FALLA_DETECTADA))
            .ToList();
        var herramienta = new ListarPanelesEnFallaTool(CrearPanelService(paneles));

        var json = await herramienta.ObtenerAsync();

        using var documento = JsonDocument.Parse(json);
        var raiz = documento.RootElement;
        Assert.Equal(201, raiz.GetProperty("cantidadEnFalla").GetInt32());
        Assert.Equal(200, raiz.GetProperty("paneles").GetArrayLength());
        Assert.Contains("201", raiz.GetProperty("aviso").GetString());
    }

    // ---------------------------------------------------------------- obtener_generacion_hoy

    [Fact]
    public async Task ObtenerGeneracionHoy_SinLecturas_ExplicaQueTodaviaNoHayDatos()
    {
        var herramienta = new ObtenerGeneracionHoyTool(CrearReporte(new TelemetriaRepositoryFalso()));

        var texto = await herramienta.ObtenerAsync();

        Assert.Contains("Todavía no hay lecturas", texto);
    }

    [Fact]
    public async Task ObtenerGeneracionHoy_ConLecturas_DevuelveElJsonConLaEnergiaEstimada()
    {
        var telemetria = new TelemetriaRepositoryFalso
        {
            Agregados = [Agregado(new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc), lecturas: 50)]
        };
        var herramienta = new ObtenerGeneracionHoyTool(CrearReporte(telemetria));

        var json = await herramienta.ObtenerAsync();

        using var documento = JsonDocument.Parse(json);
        var raiz = documento.RootElement;
        Assert.Equal("hoy", raiz.GetProperty("etiqueta").GetString());
        Assert.Equal(50, raiz.GetProperty("cantidadLecturas").GetInt32());
        Assert.Equal(1.0, raiz.GetProperty("energiaEstimadaKWh").GetDouble(), 4);
    }

    // ---------------------------------------------------------------- obtener_generacion_historica

    private static ObtenerGeneracionHistoricaTool CrearHistorica(TelemetriaRepositoryFalso telemetria,
        ParqueSolar? parque = null)
        => new(parque is null ? CrearReporte(telemetria) : CrearReporte(telemetria, parque));

    [Fact]
    public async Task ObtenerGeneracionHistorica_ConUnIdDeParqueInvalido_ExplicaElError()
    {
        var herramienta = CrearHistorica(new TelemetriaRepositoryFalso());

        var texto = await herramienta.ObtenerAsync("xyz", "dia");

        Assert.Contains("identificador del parque no es válido", texto);
    }

    [Theory]
    [InlineData("semana")]
    [InlineData("")]
    [InlineData(null)]
    public async Task ObtenerGeneracionHistorica_ConUnaAgrupacionInvalida_ExplicaLosValoresPosibles(
        string? agrupacion)
    {
        var parque = Fixture.Parque();
        var herramienta = CrearHistorica(new TelemetriaRepositoryFalso(), parque);

        var texto = await herramienta.ObtenerAsync(parque.Id.ToString(), agrupacion!);

        Assert.Contains("La agrupación no es válida", texto);
    }

    [Theory]
    [InlineData("01/11/2026", null, "La fecha desde no es válida")]
    [InlineData("2026-13-01", null, "La fecha desde no es válida")]
    [InlineData(null, "ayer", "La fecha hasta no es válida")]
    public async Task ObtenerGeneracionHistorica_ConUnaFechaInvalida_ExplicaElFormato(string? desde, string? hasta,
        string mensajeEsperado)
    {
        var parque = Fixture.Parque();
        var herramienta = CrearHistorica(new TelemetriaRepositoryFalso(), parque);

        var texto = await herramienta.ObtenerAsync(parque.Id.ToString(), "dia", desde, hasta);

        Assert.Contains(mensajeEsperado, texto);
        Assert.Contains("yyyy-MM-dd", texto);
    }

    [Fact]
    public async Task ObtenerGeneracionHistorica_SiElParqueNoExiste_DevuelveElMensajeDeLaRegla()
    {
        var herramienta = CrearHistorica(new TelemetriaRepositoryFalso());

        var texto = await herramienta.ObtenerAsync(Guid.NewGuid().ToString(), "dia");

        Assert.Equal("El parque indicado no existe.", texto);
    }

    [Fact]
    public async Task ObtenerGeneracionHistorica_ConElRangoInvertido_DevuelveElMensajeDeLaRegla()
    {
        var parque = Fixture.Parque();
        var herramienta = CrearHistorica(new TelemetriaRepositoryFalso(), parque);

        var texto = await herramienta.ObtenerAsync(parque.Id.ToString(), "dia", "2026-11-10", "2026-11-01");

        Assert.Contains("fecha desde no puede ser posterior", texto);
    }

    [Fact]
    public async Task ObtenerGeneracionHistorica_SinLecturasEnElRango_Avisa()
    {
        var parque = Fixture.Parque();
        var herramienta = CrearHistorica(new TelemetriaRepositoryFalso(), parque);

        var texto = await herramienta.ObtenerAsync(parque.Id.ToString(), "mes");

        Assert.Contains("No hay lecturas", texto);
    }

    [Fact]
    public async Task ObtenerGeneracionHistorica_ConMasDe400Periodos_PideAcotarElRango()
    {
        var parque = Fixture.Parque();
        var inicio = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var telemetria = new TelemetriaRepositoryFalso
        {
            Agregados = Enumerable.Range(0, 401).Select(dia => Agregado(inicio.AddDays(dia))).ToList()
        };
        var herramienta = CrearHistorica(telemetria, parque);

        var texto = await herramienta.ObtenerAsync(parque.Id.ToString(), "dia");

        Assert.Contains("401", texto);
        Assert.Contains("Acotá el rango", texto);
    }

    [Fact]
    public async Task ObtenerGeneracionHistorica_ConDatos_DevuelveElJsonConLosTotales()
    {
        var parque = Fixture.Parque();
        var telemetria = new TelemetriaRepositoryFalso
        {
            Agregados =
            [
                Agregado(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), lecturas: 10),
                Agregado(new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc), lecturas: 20)
            ]
        };
        var herramienta = CrearHistorica(telemetria, parque);

        var json = await herramienta.ObtenerAsync(parque.Id.ToString(), "dia", "2026-11-01", "2026-11-02");

        using var documento = JsonDocument.Parse(json);
        var raiz = documento.RootElement;
        Assert.Equal("dia", raiz.GetProperty("agrupacion").GetString());
        Assert.Equal(30, raiz.GetProperty("totalLecturas").GetInt32());
        Assert.Equal(2.0, raiz.GetProperty("totalEnergiaEstimadaKWh").GetDouble(), 4);
        var periodos = raiz.GetProperty("periodos");
        Assert.Equal(2, periodos.GetArrayLength());
        Assert.Equal("01/11/2026", periodos[0].GetProperty("etiqueta").GetString());
    }

    [Fact]
    public async Task ObtenerGeneracionHistorica_PasaLasFechasAlRepositorioIncluyendoElDiaHastaCompleto()
    {
        var parque = Fixture.Parque();
        var telemetria = new TelemetriaRepositoryFalso();
        var herramienta = CrearHistorica(telemetria, parque);

        await herramienta.ObtenerAsync(parque.Id.ToString(), "dia", " 2026-11-01 ", "2026-11-02");

        Assert.Equal(parque.Id, telemetria.UltimoParqueId);
        Assert.Equal(new DateTime(2026, 11, 1), telemetria.UltimoDesde);
        Assert.Equal(new DateTime(2026, 11, 2).Add(TimeOnly.MaxValue.ToTimeSpan()), telemetria.UltimoHasta);
    }

    [Theory]
    [InlineData("dia", GranularidadConsulta.DIA)]
    [InlineData("DÍA", GranularidadConsulta.DIA)]
    [InlineData(" Mes ", GranularidadConsulta.MES)]
    [InlineData("anio", GranularidadConsulta.ANIO)]
    [InlineData("año", GranularidadConsulta.ANIO)]
    public async Task ObtenerGeneracionHistorica_AceptaLaAgrupacionConTildeYSinDistinguirMayusculas(
        string agrupacion, GranularidadConsulta esperada)
    {
        var parque = Fixture.Parque();
        var telemetria = new TelemetriaRepositoryFalso();
        var herramienta = CrearHistorica(telemetria, parque);

        await herramienta.ObtenerAsync(parque.Id.ToString(), agrupacion);

        Assert.Equal(esperada, telemetria.UltimaGranularidad);
    }

    [Theory]
    [InlineData("mes", "mes")]
    [InlineData("año", "anio")]
    public async Task ObtenerGeneracionHistorica_NombraLaAgrupacionEnLaSalida(string pedida, string esperada)
    {
        var parque = Fixture.Parque();
        var telemetria = new TelemetriaRepositoryFalso
        {
            Agregados = [Agregado(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc))]
        };
        var herramienta = CrearHistorica(telemetria, parque);

        var json = await herramienta.ObtenerAsync(parque.Id.ToString(), pedida);

        using var documento = JsonDocument.Parse(json);
        Assert.Equal(esperada, documento.RootElement.GetProperty("agrupacion").GetString());
    }
}
