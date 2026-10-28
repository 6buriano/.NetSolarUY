using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.DTOs;
using SolarUY.Application.Sensors;
using SolarUY.Application.Services;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;
using SolarUY.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace SolarUY.Web.Controllers;

/// <summary>
/// ABM de paneles solares.
/// </summary>
[Authorize(Roles = "Administrador")]
public class PanelesController : Controller
{
    /// <summary>Cuántas lecturas se muestran en la serie reciente del panel.</summary>
    private const int LecturasEnPantalla = 20;

    private readonly PanelService _panelService;
    private readonly NodoService _nodoService;
    private readonly ISensorClient _sensorClient;

    public PanelesController(PanelService panelService, NodoService nodoService, ISensorClient sensorClient)
    {
        _panelService = panelService;
        _nodoService = nodoService;
        _sensorClient = sensorClient;
    }

    public async Task<IActionResult> Index(Guid? nodoId, EstadoSalud? estado, CancellationToken cancellationToken)
    {
        var paneles = nodoId is null
            ? await _panelService.ObtenerTodosAsync(estado, cancellationToken)
            : await _panelService.ObtenerPorNodoAsync(nodoId.Value, estado, cancellationToken);

        return View(new PanelesIndexViewModel
        {
            Paneles = paneles,
            Nodos = await _nodoService.ObtenerTodosAsync(cancellationToken),
            NodoId = nodoId,
            Estado = estado
        });
    }

    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancellationToken)
    {
        var panel = await _panelService.ObtenerPorIdAsync(id, cancellationToken);
        if (panel is null)
        {
            return NotFound();
        }

        // La serie viene de la más reciente a la más antigua, así que la última lectura es la primera.
        var lecturas = await _panelService.ObtenerLecturasRecientesAsync(id, LecturasEnPantalla, cancellationToken);

        return View(new PanelDetalleViewModel
        {
            Panel = panel,
            UltimaLectura = lecturas.FirstOrDefault(),
            LecturasRecientes = lecturas
        });
    }

    /// <summary>Le pide al sensor que reactive este panel. Requiere que el nodo tenga sensor.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Activar(Guid id, CancellationToken cancellationToken)
        => EnviarOrdenPanelAsync(id, activar: true, cancellationToken);

    /// <summary>Le pide al sensor que desactive este panel. Requiere que el nodo tenga sensor.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Desactivar(Guid id, CancellationToken cancellationToken)
        => EnviarOrdenPanelAsync(id, activar: false, cancellationToken);

    private async Task<IActionResult> EnviarOrdenPanelAsync(Guid id, bool activar, CancellationToken cancellationToken)
    {
        var panel = await _panelService.ObtenerPorIdAsync(id, cancellationToken);
        if (panel is null)
        {
            return NotFound();
        }

        var nodo = await _nodoService.ObtenerPorIdAsync(panel.NodoId, cancellationToken);
        var sensor = nodo?.Inversor.Sensor;
        if (sensor is null)
        {
            TempData["MensajeError"] = "El nodo de este panel no tiene sensor ARM registrado: no hay a quién mandarle la orden.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var resultado = activar
            ? await _sensorClient.ActivarPanelAsync(sensor.UrlBase, sensor.IdentificadorHardwareARM, panel.CodigoPanel, cancellationToken)
            : await _sensorClient.DesactivarPanelAsync(sensor.UrlBase, sensor.IdentificadorHardwareARM, panel.CodigoPanel, cancellationToken);

        var accion = activar ? "activar" : "desactivar";
        TempData[resultado == ResultadoOrdenSensor.Ok || resultado == ResultadoOrdenSensor.YaAplicado
            ? "MensajeExito"
            : "MensajeError"] = resultado switch
        {
            ResultadoOrdenSensor.Ok => $"Se mandó la orden de {accion} el panel. El colector lo va a reflejar en la próxima ronda.",
            ResultadoOrdenSensor.YaAplicado => $"El panel ya estaba {(activar ? "activo" : "inactivo")}.",
            ResultadoOrdenSensor.NoEncontrado => "El simulador no reconoce este panel: puede que se haya quitado del hardware.",
            _ => "El sensor no respondió a la orden. Probá de nuevo en un momento."
        };

        return RedirectToAction(nameof(Detalle), new { id });
    }

    public async Task<IActionResult> Crear(Guid? nodoId, CancellationToken cancellationToken)
    {
        var formulario = new PanelFormViewModel { NodoId = nodoId ?? Guid.Empty };
        await CargarNodosAsync(formulario, cancellationToken);

        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(PanelFormViewModel formulario, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await CargarNodosAsync(formulario, cancellationToken);
            return View(formulario);
        }

        try
        {
            var request = new CrearPanelRequest(
                formulario.CodigoPanel,
                formulario.Modelo,
                formulario.Fabricante,
                formulario.PotenciaNominalW,
                formulario.Estado,
                formulario.Activo,
                formulario.FechaInstalacion,
                formulario.NodoId);

            var panelId = await _panelService.CrearAsync(request, cancellationToken);

            TempData["MensajeExito"] = $"El panel '{formulario.Modelo}' se creó correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = panelId });
        }
        catch (ReglaDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarNodosAsync(formulario, cancellationToken);
            return View(formulario);
        }
    }

    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        var panel = await _panelService.ObtenerPorIdAsync(id, cancellationToken);
        if (panel is null)
        {
            return NotFound();
        }

        var formulario = new PanelFormViewModel
        {
            Id = panel.Id,
            CodigoPanel = panel.CodigoPanel,
            Modelo = panel.Modelo,
            Fabricante = panel.Fabricante,
            PotenciaNominalW = panel.PotenciaNominalW,
            Estado = panel.Estado,
            Activo = panel.Activo,
            FechaInstalacion = panel.FechaInstalacion,
            NodoId = panel.NodoId
        };

        await CargarNodosAsync(formulario, cancellationToken);
        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, PanelFormViewModel formulario, CancellationToken cancellationToken)
    {
        if (id != formulario.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await CargarNodosAsync(formulario, cancellationToken);
            return View(formulario);
        }

        try
        {
            var request = new ActualizarPanelRequest(
                id,
                formulario.CodigoPanel,
                formulario.Modelo,
                formulario.Fabricante,
                formulario.PotenciaNominalW,
                formulario.Estado,
                formulario.Activo,
                formulario.FechaInstalacion,
                formulario.NodoId);

            var actualizado = await _panelService.ActualizarAsync(request, cancellationToken);
            if (!actualizado)
            {
                return NotFound();
            }

            TempData["MensajeExito"] = $"El panel '{formulario.Modelo}' se actualizó correctamente.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (ReglaDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarNodosAsync(formulario, cancellationToken);
            return View(formulario);
        }
    }

    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        var panel = await _panelService.ObtenerPorIdAsync(id, cancellationToken);
        if (panel is null)
        {
            return NotFound();
        }

        return View(panel);
    }

    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(Guid id, CancellationToken cancellationToken)
    {
        var eliminado = await _panelService.EliminarAsync(id, cancellationToken);
        if (!eliminado)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "El panel se eliminó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarNodosAsync(PanelFormViewModel formulario, CancellationToken cancellationToken)
        => formulario.Nodos = await _nodoService.ObtenerTodosAsync(cancellationToken);
}
