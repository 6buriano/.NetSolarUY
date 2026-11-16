using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.DTOs;
using SolarUY.Application.Sensors;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Web.Controllers;
using SolarUY.Web.Models;

namespace SolarUY.Web.Tests;

/// <summary>
/// Pruebas de <see cref="PanelesController"/>: listado con filtros, detalle con lecturas y mantenimientos,
/// órdenes de activar y desactivar al sensor, y el alta, edición y baja con sus avisos.
/// </summary>
public class PanelesControllerTests
{
    private const string ClaveExito = "MensajeExito";
    private const string ClaveError = "MensajeError";

    private static readonly DateTime Ahora = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

    private sealed class Contexto
    {
        public required PanelRepositoryAbmFalso Paneles { get; init; }
        public required SensorClientFalso Sensor { get; init; }
        public required PanelesController Controlador { get; init; }
    }

    private static Contexto Crear(PanelRepositoryAbmFalso paneles, NodoRepositoryAbmFalso nodos,
        SensorClientFalso? sensor = null, TelemetriaRepositoryDatosFalso? telemetria = null,
        MantenimientoRepositoryFalso? mantenimientos = null)
    {
        var cliente = sensor ?? new SensorClientFalso();
        var parques = new ParqueRepositoryAbmFalso();
        var reloj = new RelojFijoWeb(new DateTimeOffset(Ahora));

        var controlador = new PanelesController(
            new PanelService(paneles, nodos, telemetria ?? new TelemetriaRepositoryDatosFalso()),
            new NodoService(nodos, parques),
            cliente,
            new MantenimientoService(mantenimientos ?? new MantenimientoRepositoryFalso(), paneles, reloj))
        {
            TempData = FixtureWeb.NuevoTempData()
        };

        return new Contexto { Paneles = paneles, Sensor = cliente, Controlador = controlador };
    }

    private static PanelFormViewModel FormularioValido(Guid nodoId, Guid? id = null) => new()
    {
        Id = id,
        CodigoPanel = "P10",
        Modelo = "JAM72S30",
        Fabricante = "JA Solar",
        PotenciaNominalW = 560d,
        Estado = EstadoSalud.OPERATIVO,
        Activo = true,
        FechaInstalacion = new DateTime(2026, 2, 1),
        NodoId = nodoId
    };

    // ---------------------------------------------------------------- Index y Detalle

    [Fact]
    public async Task Index_SinFiltros_MuestraTodosLosPanelesYLosNodos()
    {
        var nodo = FixtureWeb.NuevoNodo(nombre: "Nodo Central");
        var contexto = Crear(
            new PanelRepositoryAbmFalso(FixtureWeb.NuevoPanel(nodo.Id, "P01"), FixtureWeb.NuevoPanel(nodo.Id, "P02")),
            new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Index(null, null, CancellationToken.None);

        var modelo = Assert.IsType<PanelesIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(2, modelo.Paneles.Count);
        Assert.Single(modelo.Nodos);
        Assert.Null(modelo.NodoId);
        Assert.Null(modelo.Estado);
    }

    [Fact]
    public async Task Index_SinNodoPeroConEstado_FiltraPorEstadoEnTodosLosNodos()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(
            new PanelRepositoryAbmFalso(FixtureWeb.NuevoPanel(nodo.Id, "P01"),
                FixtureWeb.NuevoPanel(nodo.Id, "P02", estado: EstadoSalud.FALLA_DETECTADA)),
            new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Index(null, EstadoSalud.FALLA_DETECTADA, CancellationToken.None);

        var modelo = Assert.IsType<PanelesIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("P02", Assert.Single(modelo.Paneles).CodigoPanel);
        Assert.Equal(EstadoSalud.FALLA_DETECTADA, modelo.Estado);
    }

    [Fact]
    public async Task Index_ConNodo_MuestraSoloLosPanelesDeEseNodo()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var otro = FixtureWeb.NuevoNodo(nombre: "Otro", numeroSerie: "SN-002");
        var contexto = Crear(
            new PanelRepositoryAbmFalso(FixtureWeb.NuevoPanel(nodo.Id, "P01"), FixtureWeb.NuevoPanel(otro.Id, "P01")),
            new NodoRepositoryAbmFalso(nodo, otro));

        var resultado = await contexto.Controlador.Index(nodo.Id, null, CancellationToken.None);

        var modelo = Assert.IsType<PanelesIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(nodo.Id, Assert.Single(modelo.Paneles).NodoId);
        Assert.Equal(nodo.Id, modelo.NodoId);
        Assert.Equal(2, modelo.Nodos.Count);
    }

    [Fact]
    public async Task Detalle_SiExiste_MuestraElPanelConSuUltimaLecturaYSusMantenimientos()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var panel = FixtureWeb.NuevoPanel(nodo.Id);
        var reciente = new LecturaTelemetria(panel.Id, Ahora, 8d, 40d, 320d, 41d);
        var anterior = new LecturaTelemetria(panel.Id, Ahora.AddMinutes(-5), 7d, 40d, 280d, 40d);
        var tarea = new TareaMantenimiento(panel.Id, TipoMantenimiento.LIMPIEZA, Ahora.AddDays(3), "Limpieza", "Ana");
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo),
            telemetria: new TelemetriaRepositoryDatosFalso { Ultimas = [reciente, anterior] },
            mantenimientos: new MantenimientoRepositoryFalso(tarea));

        var resultado = await contexto.Controlador.Detalle(panel.Id, CancellationToken.None);

        var modelo = Assert.IsType<PanelDetalleViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(panel.Id, modelo.Panel.Id);
        Assert.Equal(reciente.Id, modelo.UltimaLectura!.Id);
        Assert.Equal(2, modelo.LecturasRecientes.Count);
        Assert.Equal(tarea.Id, Assert.Single(modelo.Mantenimientos).Id);
    }

    [Fact]
    public async Task Detalle_SinLecturas_DejaLaUltimaLecturaNula()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var panel = FixtureWeb.NuevoPanel(nodo.Id);
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Detalle(panel.Id, CancellationToken.None);

        var modelo = Assert.IsType<PanelDetalleViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Null(modelo.UltimaLectura);
        Assert.Empty(modelo.LecturasRecientes);
        Assert.Empty(modelo.Mantenimientos);
    }

    [Fact]
    public async Task Detalle_SiNoExiste_DevuelveNotFound()
    {
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso());

        Assert.IsType<NotFoundResult>(await contexto.Controlador.Detalle(Guid.NewGuid(), CancellationToken.None));
    }

    // ---------------------------------------------------------------- Activar y desactivar

    [Fact]
    public async Task Activar_SiElPanelNoExiste_DevuelveNotFound()
    {
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso());

        Assert.IsType<NotFoundResult>(await contexto.Controlador.Activar(Guid.NewGuid(), CancellationToken.None));
        Assert.Empty(contexto.Sensor.Ordenes);
    }

    [Fact]
    public async Task Activar_SiElNodoNoTieneSensor_AvisaYNoMandaNingunaOrden()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var panel = FixtureWeb.NuevoPanel(nodo.Id);
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Activar(panel.Id, CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(PanelesController.Detalle), redireccion.ActionName);
        Assert.Equal(panel.Id, redireccion.RouteValues!["id"]);
        Assert.Contains("no tiene sensor ARM", (string)contexto.Controlador.TempData[ClaveError]!);
        Assert.Empty(contexto.Sensor.Ordenes);
    }

    [Fact]
    public async Task Activar_MandaLaOrdenAlSensorDelNodo()
    {
        var nodo = FixtureWeb.NuevoNodo(hardware: "ARM-001");
        var panel = FixtureWeb.NuevoPanel(nodo.Id, "P07");
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo));

        await contexto.Controlador.Activar(panel.Id, CancellationToken.None);

        Assert.Equal("activar-panel http://sensor-a:5100 ARM-001 P07", Assert.Single(contexto.Sensor.Ordenes));
    }

    [Fact]
    public async Task Desactivar_MandaLaOrdenDeDesactivarAlSensorDelNodo()
    {
        var nodo = FixtureWeb.NuevoNodo(hardware: "ARM-001");
        var panel = FixtureWeb.NuevoPanel(nodo.Id, "P07");
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo));

        await contexto.Controlador.Desactivar(panel.Id, CancellationToken.None);

        Assert.Equal("desactivar-panel http://sensor-a:5100 ARM-001 P07", Assert.Single(contexto.Sensor.Ordenes));
    }

    [Theory]
    [InlineData(ResultadoOrdenSensor.Ok, ClaveExito, "Se mandó la orden de activar el panel")]
    [InlineData(ResultadoOrdenSensor.YaAplicado, ClaveExito, "El panel ya estaba activo")]
    [InlineData(ResultadoOrdenSensor.NoEncontrado, ClaveError, "El simulador no reconoce este panel")]
    [InlineData(ResultadoOrdenSensor.SinRespuesta, ClaveError, "El sensor no respondió")]
    public async Task Activar_DejaElAvisoSegunLaRespuestaDelSensor(ResultadoOrdenSensor respuesta, string clave,
        string textoEsperado)
    {
        var nodo = FixtureWeb.NuevoNodo(hardware: "ARM-001");
        var panel = FixtureWeb.NuevoPanel(nodo.Id);
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo),
            new SensorClientFalso(respuesta));

        var resultado = await contexto.Controlador.Activar(panel.Id, CancellationToken.None);

        Assert.Equal(nameof(PanelesController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        Assert.Contains(textoEsperado, (string)contexto.Controlador.TempData[clave]!);
    }

    [Fact]
    public async Task Desactivar_SiElPanelYaEstabaInactivo_LoDiceEnElAviso()
    {
        var nodo = FixtureWeb.NuevoNodo(hardware: "ARM-001");
        var panel = FixtureWeb.NuevoPanel(nodo.Id);
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo),
            new SensorClientFalso(ResultadoOrdenSensor.YaAplicado));

        await contexto.Controlador.Desactivar(panel.Id, CancellationToken.None);

        Assert.Equal("El panel ya estaba inactivo.", contexto.Controlador.TempData[ClaveExito]);
    }

    // ---------------------------------------------------------------- Crear

    [Fact]
    public async Task Crear_Get_PrecargaElNodoYLaListaDeNodos()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Crear(nodo.Id, CancellationToken.None);

        var modelo = Assert.IsType<PanelFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(nodo.Id, modelo.NodoId);
        Assert.Single(modelo.Nodos);
    }

    [Fact]
    public async Task Crear_Get_SinNodo_DejaElNodoVacio()
    {
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso());

        var resultado = await contexto.Controlador.Crear((Guid?)null, CancellationToken.None);

        Assert.Equal(Guid.Empty, Assert.IsType<PanelFormViewModel>(Assert.IsType<ViewResult>(resultado).Model).NodoId);
    }

    [Fact]
    public async Task Crear_Post_ConModeloInvalido_VuelveALaVistaConLosNodosSinGuardar()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso(nodo));
        contexto.Controlador.ModelState.AddModelError("Modelo", "obligatorio");
        var formulario = FormularioValido(nodo.Id);

        var resultado = await contexto.Controlador.Crear(formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Single(formulario.Nodos);
        Assert.Empty(contexto.Paneles.Agregados);
    }

    [Fact]
    public async Task Crear_Post_ConDatosValidos_GuardaYVaAlDetalleDelPanel()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Crear(FormularioValido(nodo.Id), CancellationToken.None);

        var panel = Assert.Single(contexto.Paneles.Agregados);
        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(PanelesController.Detalle), redireccion.ActionName);
        Assert.Equal(panel.Id, redireccion.RouteValues!["id"]);
        Assert.Equal("P10", panel.CodigoPanel);
        Assert.Equal("El panel 'JAM72S30' se creó correctamente.", contexto.Controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Crear_Post_SiElNodoNoExiste_MuestraElErrorDelDominio()
    {
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso());
        var formulario = FormularioValido(Guid.NewGuid());

        var resultado = await contexto.Controlador.Crear(formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("El nodo seleccionado no existe.",
            contexto.Controlador.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        Assert.Empty(contexto.Paneles.Agregados);
    }

    // ---------------------------------------------------------------- Editar

    [Fact]
    public async Task Editar_Get_CargaElFormularioConLosDatosDelPanel()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var panel = FixtureWeb.NuevoPanel(nodo.Id, "P05", 600d, EstadoSalud.RENDIMIENTO_BAJO);
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Editar(panel.Id, CancellationToken.None);

        var modelo = Assert.IsType<PanelFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(panel.Id, modelo.Id);
        Assert.Equal("P05", modelo.CodigoPanel);
        Assert.Equal(600d, modelo.PotenciaNominalW, 6);
        Assert.Equal(EstadoSalud.RENDIMIENTO_BAJO, modelo.Estado);
        Assert.Equal(nodo.Id, modelo.NodoId);
        Assert.Single(modelo.Nodos);
    }

    [Fact]
    public async Task Editar_Get_SiNoExiste_DevuelveNotFound()
    {
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso());

        Assert.IsType<NotFoundResult>(await contexto.Controlador.Editar(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Editar_Post_SiElIdNoCoincide_DevuelveNotFound()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var panel = FixtureWeb.NuevoPanel(nodo.Id);
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Editar(panel.Id, FormularioValido(nodo.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Editar_Post_ConModeloInvalido_VuelveALaVistaSinGuardar()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var panel = FixtureWeb.NuevoPanel(nodo.Id);
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo));
        contexto.Controlador.ModelState.AddModelError("Modelo", "obligatorio");
        var formulario = FormularioValido(nodo.Id, panel.Id);

        var resultado = await contexto.Controlador.Editar(panel.Id, formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Single(formulario.Nodos);
        Assert.Empty(contexto.Paneles.Actualizados);
    }

    [Fact]
    public async Task Editar_Post_ConDatosValidos_ActualizaYVuelveAlDetalle()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var panel = FixtureWeb.NuevoPanel(nodo.Id);
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Editar(panel.Id, FormularioValido(nodo.Id, panel.Id),
            CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(PanelesController.Detalle), redireccion.ActionName);
        Assert.Equal("P10", panel.CodigoPanel);
        Assert.Single(contexto.Paneles.Actualizados);
        Assert.Equal("El panel 'JAM72S30' se actualizó correctamente.", contexto.Controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Editar_Post_SiElPanelYaNoExiste_DevuelveNotFound()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso(nodo));
        var id = Guid.NewGuid();

        var resultado = await contexto.Controlador.Editar(id, FormularioValido(nodo.Id, id), CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Editar_Post_SiElCodigoYaExiste_MuestraElErrorDelDominio()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var panel = FixtureWeb.NuevoPanel(nodo.Id);
        var contexto = Crear(new PanelRepositoryAbmFalso(panel) { CodigoOcupado = true },
            new NodoRepositoryAbmFalso(nodo));
        var formulario = FormularioValido(nodo.Id, panel.Id);

        var resultado = await contexto.Controlador.Editar(panel.Id, formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Contains("'P10'", contexto.Controlador.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        Assert.Empty(contexto.Paneles.Actualizados);
    }

    // ---------------------------------------------------------------- Eliminar

    [Fact]
    public async Task Eliminar_Get_PideConfirmacionConElPanel()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var panel = FixtureWeb.NuevoPanel(nodo.Id);
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Eliminar(panel.Id, CancellationToken.None);

        Assert.Equal(panel.Id, Assert.IsType<PanelDto>(Assert.IsType<ViewResult>(resultado).Model).Id);
    }

    [Fact]
    public async Task Eliminar_Get_SiNoExiste_DevuelveNotFound()
    {
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso());

        Assert.IsType<NotFoundResult>(await contexto.Controlador.Eliminar(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarConfirmado_SiExiste_LoEliminaYVuelveAlListado()
    {
        var panel = FixtureWeb.NuevoPanel(Guid.NewGuid());
        var contexto = Crear(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso());

        var resultado = await contexto.Controlador.EliminarConfirmado(panel.Id, CancellationToken.None);

        Assert.Equal(nameof(PanelesController.Index), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        Assert.Same(panel, Assert.Single(contexto.Paneles.Eliminados));
        Assert.Equal("El panel se eliminó correctamente.", contexto.Controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task EliminarConfirmado_SiNoExiste_DevuelveNotFound()
    {
        var contexto = Crear(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso());

        Assert.IsType<NotFoundResult>(
            await contexto.Controlador.EliminarConfirmado(Guid.NewGuid(), CancellationToken.None));
    }

    private sealed class RelojFijoWeb(DateTimeOffset momento) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => momento;
    }
}
