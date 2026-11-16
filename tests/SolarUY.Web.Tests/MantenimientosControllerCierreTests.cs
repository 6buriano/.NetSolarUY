using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Web.Controllers;
using SolarUY.Web.Models;

namespace SolarUY.Web.Tests;

/// <summary>
/// Pruebas del cierre de una tarea cuando la fecha de realización puede venir vacía. La fecha del
/// formulario es opcional para el enlazador de modelos (<c>DateTime?</c>) y obligatoria por validación; el
/// controlador tiene que resistir también el caso en que el modelo llegue sin fecha.
/// </summary>
public class MantenimientosControllerCierreTests
{
    private static readonly DateTime Ahora = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);

    private static MantenimientosController CrearControlador(MantenimientoRepositoryFalso tareas)
    {
        var paneles = new PanelRepositoryFalso();
        var mantenimientoService = new MantenimientoService(tareas, paneles, new RelojFijo(Ahora));
        var panelService = new PanelService(paneles, new NodoRepositoryFalso(), new TelemetriaRepositoryFalso());

        return new MantenimientosController(mantenimientoService, panelService)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TempDataProviderVacio())
        };
    }

    [Fact]
    public async Task Completar_SiElModeloLlegaSinFecha_AvisaElErrorYNoTocaLaTarea()
    {
        var tarea = new TareaMantenimiento(Guid.NewGuid(), TipoMantenimiento.LIMPIEZA,
            new DateTime(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc), "Limpieza general", "Ana");
        tarea.Iniciar();
        var tareas = new MantenimientoRepositoryFalso(tarea);
        var controlador = CrearControlador(tareas);

        // El ModelState queda válido a propósito: lo que se prueba es la protección propia del controlador.
        var resultado = await controlador.Completar(tarea.Id, new CierreMantenimientoViewModel(),
            CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(MantenimientosController.Detalle), redireccion.ActionName);
        Assert.NotNull(controlador.TempData["MensajeError"]);
        Assert.Equal(EstadoMantenimiento.EN_PROCESO, tarea.Estado);
        Assert.Equal(0, tareas.Actualizaciones);
    }

    private sealed class RelojFijo(DateTime momento) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(momento);
    }

    private sealed class TempDataProviderVacio : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
