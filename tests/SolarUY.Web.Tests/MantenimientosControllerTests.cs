using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Web.Controllers;
using SolarUY.Web.Models;

namespace SolarUY.Web.Tests;

/// <summary>
/// Pruebas de <see cref="MantenimientosController"/>: a qué pantalla redirige y qué aviso deja en
/// TempData según lo que responda el servicio. Se usan los servicios reales con repositorios falsos.
/// </summary>
public class MantenimientosControllerTests
{
    private const string ClaveExito = "MensajeExito";
    private const string ClaveError = "MensajeError";

    private static readonly DateTime Ahora = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaProgramada = new(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc);

    private static PanelSolar NuevoPanel()
        => new("P01", "CS6P-300", "Canadian Solar", 300, EstadoSalud.OPERATIVO,
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());

    private static TareaMantenimiento NuevaTarea(Guid? panelId = null)
        => new(panelId ?? Guid.NewGuid(), TipoMantenimiento.LIMPIEZA, FechaProgramada, "Limpieza general", "Ana");

    private static TareaMantenimiento TareaEnProceso(Guid? panelId = null)
    {
        var tarea = NuevaTarea(panelId);
        tarea.Iniciar();
        return tarea;
    }

    private static TareaMantenimiento TareaCompletada()
    {
        var tarea = TareaEnProceso();
        tarea.Completar(Ahora.AddHours(-2), "Listo", Ahora);
        return tarea;
    }

    private static MantenimientosController CrearControlador(MantenimientoRepositoryFalso tareas,
        PanelRepositoryFalso? paneles = null)
    {
        var repositorioPaneles = paneles ?? new PanelRepositoryFalso();
        var mantenimientoService = new MantenimientoService(tareas, repositorioPaneles,
            new RelojFijo(new DateTimeOffset(Ahora)));
        var panelService = new PanelService(repositorioPaneles, new NodoRepositoryFalso(),
            new TelemetriaRepositoryFalso());

        return new MantenimientosController(mantenimientoService, panelService)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TempDataProviderFalso())
        };
    }

    private static MantenimientoFormViewModel FormularioValido(Guid panelId) => new()
    {
        PanelSolarId = panelId,
        Tipo = TipoMantenimiento.LIMPIEZA,
        FechaProgramada = FechaProgramada,
        Descripcion = "Limpieza general",
        PersonalAsignado = "Ana"
    };

    private static void AssertRedirigeADetalle(IActionResult resultado, Guid id)
    {
        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(MantenimientosController.Detalle), redireccion.ActionName);
        Assert.Equal(id, redireccion.RouteValues!["id"]);
    }

    // ---------------------------------------------------------------- Index y Detalle

    [Fact]
    public async Task Index_SinFiltros_DevuelveTodasLasTareasYLosPaneles()
    {
        var controlador = CrearControlador(
            new MantenimientoRepositoryFalso(NuevaTarea(), NuevaTarea()), new PanelRepositoryFalso(NuevoPanel()));

        var resultado = await controlador.Index(null, null, CancellationToken.None);

        var modelo = Assert.IsType<MantenimientosIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(2, modelo.Tareas.Count);
        Assert.Single(modelo.Paneles);
    }

    [Fact]
    public async Task Index_ConPanel_DejaSoloLasTareasDeEsePanel()
    {
        var delPanel = NuevaTarea();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(delPanel, NuevaTarea()));

        var resultado = await controlador.Index(delPanel.PanelSolarId, null, CancellationToken.None);

        var modelo = Assert.IsType<MantenimientosIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(delPanel.Id, Assert.Single(modelo.Tareas).Id);
        Assert.Equal(delPanel.PanelSolarId, modelo.PanelSolarId);
    }

    [Fact]
    public async Task Index_ConEstado_SeLoPasaAlRepositorio()
    {
        var tareas = new MantenimientoRepositoryFalso(NuevaTarea());
        var controlador = CrearControlador(tareas);

        var resultado = await controlador.Index(null, EstadoMantenimiento.EN_PROCESO, CancellationToken.None);

        var modelo = Assert.IsType<MantenimientosIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(EstadoMantenimiento.EN_PROCESO, tareas.UltimoEstadoPedido);
        Assert.Equal(EstadoMantenimiento.EN_PROCESO, modelo.Estado);
    }

    [Fact]
    public async Task Detalle_SiLaTareaNoExiste_DevuelveNotFound()
    {
        var controlador = CrearControlador(new MantenimientoRepositoryFalso());

        var resultado = await controlador.Detalle(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Detalle_SiLaTareaExiste_DevuelveLaVistaConLaTarea()
    {
        var tarea = NuevaTarea();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(tarea));

        var resultado = await controlador.Detalle(tarea.Id, CancellationToken.None);

        var dto = Assert.IsType<MantenimientoDto>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(tarea.Id, dto.Id);
    }

    // ---------------------------------------------------------------- Crear

    [Fact]
    public async Task Crear_Get_PrecargaElPanelIndicadoYLaListaDePaneles()
    {
        var panel = NuevoPanel();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(), new PanelRepositoryFalso(panel));

        var resultado = await controlador.Crear(panel.Id, CancellationToken.None);

        var formulario = Assert.IsType<MantenimientoFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(panel.Id, formulario.PanelSolarId);
        Assert.Single(formulario.Paneles);
    }

    [Fact]
    public async Task Crear_Get_SinPanel_DejaElPanelSinElegir()
    {
        var controlador = CrearControlador(new MantenimientoRepositoryFalso());

        var resultado = await controlador.Crear((Guid?)null, CancellationToken.None);

        var formulario = Assert.IsType<MantenimientoFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(Guid.Empty, formulario.PanelSolarId);
    }

    [Fact]
    public async Task Crear_Post_ConModeloInvalido_VuelveAlFormularioSinGuardar()
    {
        var tareas = new MantenimientoRepositoryFalso();
        var controlador = CrearControlador(tareas, new PanelRepositoryFalso(NuevoPanel()));
        controlador.ModelState.AddModelError(nameof(MantenimientoFormViewModel.Descripcion), "Obligatoria.");

        var resultado = await controlador.Crear(new MantenimientoFormViewModel(), CancellationToken.None);

        var formulario = Assert.IsType<MantenimientoFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Single(formulario.Paneles);
        Assert.Empty(tareas.Agregadas);
    }

    [Fact]
    public async Task Crear_Post_ConDatosValidos_GuardaLaTareaYRedirigeAlDetalle()
    {
        var panel = NuevoPanel();
        var tareas = new MantenimientoRepositoryFalso();
        var controlador = CrearControlador(tareas, new PanelRepositoryFalso(panel));

        var resultado = await controlador.Crear(FormularioValido(panel.Id), CancellationToken.None);

        var tarea = Assert.Single(tareas.Agregadas);
        AssertRedirigeADetalle(resultado, tarea.Id);
        Assert.NotNull(controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Crear_Post_SiElPanelNoExiste_MuestraElErrorEnElFormulario()
    {
        var tareas = new MantenimientoRepositoryFalso();
        var controlador = CrearControlador(tareas);

        var resultado = await controlador.Crear(FormularioValido(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controlador.ModelState.IsValid);
        Assert.Empty(tareas.Agregadas);
    }

    // ---------------------------------------------------------------- Editar

    [Fact]
    public async Task Editar_Get_SiLaTareaNoExiste_DevuelveNotFound()
    {
        var controlador = CrearControlador(new MantenimientoRepositoryFalso());

        var resultado = await controlador.Editar(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Editar_Get_SiLaTareaYaSeCompleto_AvisaYVuelveAlDetalle()
    {
        var tarea = TareaCompletada();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(tarea));

        var resultado = await controlador.Editar(tarea.Id, CancellationToken.None);

        AssertRedirigeADetalle(resultado, tarea.Id);
        Assert.NotNull(controlador.TempData[ClaveError]);
    }

    [Fact]
    public async Task Editar_Get_ConUnaTareaPendiente_CargaElFormularioConSusDatos()
    {
        var tarea = NuevaTarea();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(tarea));

        var resultado = await controlador.Editar(tarea.Id, CancellationToken.None);

        var formulario = Assert.IsType<MantenimientoFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(tarea.Id, formulario.Id);
        Assert.Equal("Limpieza general", formulario.Descripcion);
        Assert.Equal("Ana", formulario.PersonalAsignado);
    }

    [Fact]
    public async Task Editar_Post_SiElIdNoCoincideConElDelFormulario_DevuelveNotFound()
    {
        var controlador = CrearControlador(new MantenimientoRepositoryFalso());
        var formulario = FormularioValido(Guid.NewGuid());
        formulario.Id = Guid.NewGuid();

        var resultado = await controlador.Editar(Guid.NewGuid(), formulario, CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Editar_Post_ConModeloInvalido_VuelveAlFormularioSinActualizar()
    {
        var tarea = NuevaTarea();
        var tareas = new MantenimientoRepositoryFalso(tarea);
        var controlador = CrearControlador(tareas);
        controlador.ModelState.AddModelError(nameof(MantenimientoFormViewModel.Descripcion), "Obligatoria.");
        var formulario = FormularioValido(tarea.PanelSolarId);
        formulario.Id = tarea.Id;

        var resultado = await controlador.Editar(tarea.Id, formulario, CancellationToken.None);

        Assert.IsType<ViewResult>(resultado);
        Assert.Equal(0, tareas.Actualizaciones);
    }

    [Fact]
    public async Task Editar_Post_ConDatosValidos_ReprogramaYRedirigeAlDetalle()
    {
        var tarea = NuevaTarea();
        var tareas = new MantenimientoRepositoryFalso(tarea);
        var controlador = CrearControlador(tareas);
        var formulario = FormularioValido(tarea.PanelSolarId);
        formulario.Id = tarea.Id;
        formulario.Descripcion = "Reprogramada";

        var resultado = await controlador.Editar(tarea.Id, formulario, CancellationToken.None);

        AssertRedirigeADetalle(resultado, tarea.Id);
        Assert.Equal("Reprogramada", tarea.Descripcion);
        Assert.Equal(1, tareas.Actualizaciones);
        Assert.NotNull(controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Editar_Post_SiLaTareaNoExiste_DevuelveNotFound()
    {
        var id = Guid.NewGuid();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso());
        var formulario = FormularioValido(Guid.NewGuid());
        formulario.Id = id;

        var resultado = await controlador.Editar(id, formulario, CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Editar_Post_SiLaTareaYaSeCompleto_MuestraElErrorEnElFormulario()
    {
        var tarea = TareaCompletada();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(tarea));
        var formulario = FormularioValido(tarea.PanelSolarId);
        formulario.Id = tarea.Id;

        var resultado = await controlador.Editar(tarea.Id, formulario, CancellationToken.None);

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controlador.ModelState.IsValid);
    }

    // ---------------------------------------------------------------- Cambios de estado

    [Fact]
    public async Task Iniciar_ConUnaTareaPendiente_LaPoneEnProcesoYAvisa()
    {
        var tarea = NuevaTarea();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(tarea));

        var resultado = await controlador.Iniciar(tarea.Id, CancellationToken.None);

        AssertRedirigeADetalle(resultado, tarea.Id);
        Assert.Equal(EstadoMantenimiento.EN_PROCESO, tarea.Estado);
        Assert.NotNull(controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Iniciar_SiLaTareaNoExiste_DevuelveNotFound()
    {
        var controlador = CrearControlador(new MantenimientoRepositoryFalso());

        var resultado = await controlador.Iniciar(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Iniciar_SiLaTareaYaEstabaEnProceso_AvisaElErrorSinCambiarNada()
    {
        var tarea = TareaEnProceso();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(tarea));

        var resultado = await controlador.Iniciar(tarea.Id, CancellationToken.None);

        AssertRedirigeADetalle(resultado, tarea.Id);
        Assert.NotNull(controlador.TempData[ClaveError]);
        Assert.Null(controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Cancelar_ConUnaTareaPendiente_LaCancelaYAvisa()
    {
        var tarea = NuevaTarea();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(tarea));

        var resultado = await controlador.Cancelar(tarea.Id, CancellationToken.None);

        AssertRedirigeADetalle(resultado, tarea.Id);
        Assert.Equal(EstadoMantenimiento.CANCELADO, tarea.Estado);
        Assert.NotNull(controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Cancelar_SiLaTareaYaSeCompleto_AvisaElError()
    {
        var tarea = TareaCompletada();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(tarea));

        var resultado = await controlador.Cancelar(tarea.Id, CancellationToken.None);

        AssertRedirigeADetalle(resultado, tarea.Id);
        Assert.NotNull(controlador.TempData[ClaveError]);
    }

    [Fact]
    public async Task Cancelar_SiLaTareaNoExiste_DevuelveNotFound()
    {
        var controlador = CrearControlador(new MantenimientoRepositoryFalso());

        var resultado = await controlador.Cancelar(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    // ---------------------------------------------------------------- Completar

    [Fact]
    public async Task Completar_ConModeloInvalido_AvisaElErrorSinTocarLaTarea()
    {
        var tarea = TareaEnProceso();
        var tareas = new MantenimientoRepositoryFalso(tarea);
        var controlador = CrearControlador(tareas);
        controlador.ModelState.AddModelError(nameof(CierreMantenimientoViewModel.FechaRealizada), "Obligatoria.");

        var resultado = await controlador.Completar(tarea.Id, new CierreMantenimientoViewModel(),
            CancellationToken.None);

        AssertRedirigeADetalle(resultado, tarea.Id);
        Assert.NotNull(controlador.TempData[ClaveError]);
        Assert.Equal(0, tareas.Actualizaciones);
    }

    [Fact]
    public async Task Completar_ConDatosValidos_ConvierteLaHoraLocalAUtcYCompleta()
    {
        var tarea = TareaEnProceso();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(tarea));
        // 09:00 en Montevideo (UTC-3) son las 12:00 UTC, anterior al "ahora" de la prueba (15:00 UTC).
        var cierre = new CierreMantenimientoViewModel
        {
            FechaRealizada = new DateTime(2026, 11, 11, 9, 0, 0),
            ObservacionesCierre = "Se limpió el panel"
        };

        var resultado = await controlador.Completar(tarea.Id, cierre, CancellationToken.None);

        AssertRedirigeADetalle(resultado, tarea.Id);
        Assert.Equal(EstadoMantenimiento.COMPLETADO, tarea.Estado);
        Assert.Equal(new DateTime(2026, 11, 11, 12, 0, 0), tarea.FechaRealizada!.Value);
        Assert.NotNull(controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Completar_SiLaTareaNoExiste_DevuelveNotFound()
    {
        var controlador = CrearControlador(new MantenimientoRepositoryFalso());
        var cierre = new CierreMantenimientoViewModel { FechaRealizada = new DateTime(2026, 11, 11, 9, 0, 0) };

        var resultado = await controlador.Completar(Guid.NewGuid(), cierre, CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Completar_SiLaTareaNoEstabaEnProceso_AvisaElError()
    {
        var tarea = NuevaTarea();
        var controlador = CrearControlador(new MantenimientoRepositoryFalso(tarea));
        var cierre = new CierreMantenimientoViewModel { FechaRealizada = new DateTime(2026, 11, 11, 9, 0, 0) };

        var resultado = await controlador.Completar(tarea.Id, cierre, CancellationToken.None);

        AssertRedirigeADetalle(resultado, tarea.Id);
        Assert.NotNull(controlador.TempData[ClaveError]);
        Assert.Equal(EstadoMantenimiento.PENDIENTE, tarea.Estado);
    }

    // ---------------------------------------------------------------- Dobles de prueba

    private sealed class RelojFijo(DateTimeOffset momento) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => momento;
    }

    /// <summary>TempData en memoria: no hay sesión ni cookie, solo el diccionario.</summary>
    private sealed class TempDataProviderFalso : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
