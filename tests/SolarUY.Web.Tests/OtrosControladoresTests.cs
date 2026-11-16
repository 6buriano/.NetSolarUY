using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.Diagnostics;
using SolarUY.Application.Sensors;
using SolarUY.Application.Services;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;
using SolarUY.Web.Controllers;
using SolarUY.Web.Models;

namespace SolarUY.Web.Tests;

/// <summary>
/// Pruebas de los controladores chicos: Histórico (generación por panel), Diagnóstico (solo en desarrollo),
/// Home y Ayuda.
/// </summary>
public class OtrosControladoresTests
{
    private static readonly DateTime Dia = new(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);

    private static HistoricoController CrearHistorico(PanelRepositoryAbmFalso paneles,
        TelemetriaRepositoryDatosFalso telemetria)
        => new(
            new ReporteService(telemetria, new ParqueRepositoryAbmFalso(), TimeProvider.System),
            new PanelService(paneles, new NodoRepositoryAbmFalso(), telemetria));

    // ---------------------------------------------------------------- Histórico

    [Fact]
    public async Task Historico_SinPanelesCargados_NoMuestraResultados()
    {
        var telemetria = new TelemetriaRepositoryDatosFalso();
        var controlador = CrearHistorico(new PanelRepositoryAbmFalso(), telemetria);

        var resultado = await controlador.Index(null);

        var modelo = Assert.IsType<HistoricoIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Null(modelo.PanelSolarId);
        Assert.Empty(modelo.Paneles);
        Assert.Empty(modelo.Resultados);
        Assert.Null(telemetria.UltimoPanelId);
    }

    [Fact]
    public async Task Historico_SinPanelElegido_ProponeElPrimeroYConsultaPorDia()
    {
        var primero = FixtureWeb.NuevoPanel(Guid.NewGuid(), "P01");
        var segundo = FixtureWeb.NuevoPanel(Guid.NewGuid(), "P02");
        var telemetria = new TelemetriaRepositoryDatosFalso
        {
            Agregados = [new AgregadoPotencia(Dia, 720, 300d, 450d, 216000d)]
        };
        var controlador = CrearHistorico(new PanelRepositoryAbmFalso(primero, segundo), telemetria);

        var resultado = await controlador.Index(null);

        var modelo = Assert.IsType<HistoricoIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(primero.Id, modelo.PanelSolarId);
        Assert.Equal(2, modelo.Paneles.Count);
        Assert.Equal(GranularidadConsulta.DIA, modelo.Granularidad);
        Assert.Equal(primero.Id, telemetria.UltimoPanelId);
        var fila = Assert.Single(modelo.Resultados);
        Assert.Equal(720, fila.CantidadLecturas);
        Assert.Equal(300d, fila.PotenciaPromedioW, 6);
    }

    [Fact]
    public async Task Historico_ConPanelYFechas_ConsultaEseRangoHastaElFinalDelDiaPedido()
    {
        var panel = FixtureWeb.NuevoPanel(Guid.NewGuid());
        var telemetria = new TelemetriaRepositoryDatosFalso();
        var controlador = CrearHistorico(new PanelRepositoryAbmFalso(FixtureWeb.NuevoPanel(Guid.NewGuid()), panel),
            telemetria);
        var desde = new DateTime(2026, 11, 1);
        var hasta = new DateTime(2026, 11, 5, 14, 30, 0);

        var resultado = await controlador.Index(panel.Id, GranularidadConsulta.MES, desde, hasta);

        var modelo = Assert.IsType<HistoricoIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(panel.Id, modelo.PanelSolarId);
        Assert.Equal(GranularidadConsulta.MES, modelo.Granularidad);
        Assert.Equal(desde, modelo.Desde);
        Assert.Equal(hasta, modelo.Hasta);
        Assert.Equal(panel.Id, telemetria.UltimoPanelId);
        Assert.Equal(GranularidadConsulta.MES, telemetria.UltimaGranularidad);
        Assert.Equal(desde, telemetria.UltimoDesde);
        Assert.Equal(new DateTime(2026, 11, 5).AddDays(1).AddTicks(-1), telemetria.UltimoHasta);
    }

    [Fact]
    public async Task Historico_SinFechaHasta_NoAcotaLaConsulta()
    {
        var panel = FixtureWeb.NuevoPanel(Guid.NewGuid());
        var telemetria = new TelemetriaRepositoryDatosFalso();
        var controlador = CrearHistorico(new PanelRepositoryAbmFalso(panel), telemetria);

        await controlador.Index(panel.Id);

        Assert.Null(telemetria.UltimoDesde);
        Assert.Null(telemetria.UltimoHasta);
    }

    // ---------------------------------------------------------------- Diagnóstico

    private static DiagnosticoController CrearDiagnostico(string entorno, SimuladorAdminFalso simulador,
        string? urlSimulador = null)
        => new(
            new VerificacionVinculacionService(new NodoRepositoryAbmFalso(), simulador),
            new EntornoWebFalso(entorno),
            FixtureWeb.Configuracion(urlSimulador));

    [Fact]
    public async Task Diagnostico_FueraDeDesarrollo_DevuelveNotFoundSinConsultarAlSimulador()
    {
        var simulador = new SimuladorAdminFalso(null);
        var controlador = CrearDiagnostico("Production", simulador);

        var resultado = await controlador.Vinculacion(CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
        Assert.Null(simulador.UrlPedida);
    }

    [Fact]
    public async Task Diagnostico_EnDesarrollo_ConsultaElSimuladorConfiguradoYMuestraElInforme()
    {
        var simulador = new SimuladorAdminFalso([]);
        var controlador = CrearDiagnostico("Development", simulador, "http://simulador:5100");

        var resultado = await controlador.Vinculacion(CancellationToken.None);

        var vista = Assert.IsType<ViewResult>(resultado);
        var informe = Assert.IsType<InformeVinculacion>(vista.Model);
        Assert.True(informe.SimuladorConsultado);
        Assert.Equal("http://simulador:5100", simulador.UrlPedida);
        Assert.Equal("http://simulador:5100", vista.ViewData["UrlSimulador"]);
        Assert.Equal("Verificación de vinculación", vista.ViewData["TituloPagina"]);
    }

    [Fact]
    public async Task Diagnostico_SinSimuladorConfigurado_UsaLaDireccionLocalPorDefecto()
    {
        var simulador = new SimuladorAdminFalso([]);
        var controlador = CrearDiagnostico("Development", simulador);

        await controlador.Vinculacion(CancellationToken.None);

        Assert.Equal("http://localhost:5100", simulador.UrlPedida);
    }

    [Fact]
    public async Task Diagnostico_SiElSimuladorNoResponde_MuestraElInformeConElMotivo()
    {
        var controlador = CrearDiagnostico("Development", new SimuladorAdminFalso(null));

        var resultado = await controlador.Vinculacion(CancellationToken.None);

        var informe = Assert.IsType<InformeVinculacion>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.False(informe.SimuladorConsultado);
        Assert.NotNull(informe.ErrorConsulta);
    }

    // ---------------------------------------------------------------- Home y Ayuda

    [Fact]
    public void Home_Index_RedirigeAlMonitoreo()
    {
        var resultado = new HomeController().Index();

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redireccion.ActionName);
        Assert.Equal("Monitoreo", redireccion.ControllerName);
    }

    [Fact]
    public void Home_Privacy_MuestraLaVista()
    {
        Assert.IsType<ViewResult>(new HomeController().Privacy());
    }

    [Fact]
    public void Home_Error_InformaElIdentificadorDeLaSolicitud()
    {
        var contexto = new DefaultHttpContext { TraceIdentifier = "traza-123" };
        var controlador = new HomeController { ControllerContext = new ControllerContext { HttpContext = contexto } };

        var resultado = controlador.Error();

        var modelo = Assert.IsType<ErrorViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("traza-123", modelo.RequestId);
        Assert.True(modelo.ShowRequestId);
    }

    [Fact]
    public void ErrorViewModel_SinIdentificador_NoLoMuestra()
    {
        Assert.False(new ErrorViewModel().ShowRequestId);
        Assert.False(new ErrorViewModel { RequestId = string.Empty }.ShowRequestId);
    }

    [Fact]
    public void Ayuda_Index_MuestraLaVista()
    {
        Assert.IsType<ViewResult>(new AyudaController().Index());
    }

    private sealed class SimuladorAdminFalso(IReadOnlyList<InventarioSensorDto>? inventario) : ISimuladorAdminClient
    {
        public string? UrlPedida { get; private set; }

        public Task<IReadOnlyList<InventarioSensorDto>?> ObtenerInventarioAsync(string urlBase,
            CancellationToken cancellationToken = default)
        {
            UrlPedida = urlBase;
            return Task.FromResult(inventario);
        }

        public Task<InventarioSensorDto?> CrearSensorAsync(string urlBase, string identificadorHardwareARM,
            int cantidadPaneles, double potenciaNominalW, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
