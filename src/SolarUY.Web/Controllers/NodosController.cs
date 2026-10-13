using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Exceptions;
using SolarUY.Web.Models;

namespace SolarUY.Web.Controllers;

/// <summary>
/// ABM de nodos. Un nodo arrastra siempre los datos de su inversor (regla de negocio 1).
/// </summary>
// TODO (etapa de seguridad): agregar [Authorize(Roles = "Administrador")] cuando esté Keycloak.
public class NodosController : Controller
{
    private readonly NodoService _nodoService;
    private readonly ParqueService _parqueService;
    private readonly PanelService _panelService;

    public NodosController(NodoService nodoService, ParqueService parqueService, PanelService panelService)
    {
        _nodoService = nodoService;
        _parqueService = parqueService;
        _panelService = panelService;
    }

    public async Task<IActionResult> Index(Guid? parqueId, CancellationToken cancellationToken)
    {
        var nodos = parqueId is null
            ? await _nodoService.ObtenerTodosAsync(cancellationToken)
            : await _nodoService.ObtenerPorParqueAsync(parqueId.Value, cancellationToken);

        return View(new NodosIndexViewModel
        {
            Nodos = nodos,
            Parques = await _parqueService.ObtenerTodosAsync(cancellationToken),
            ParqueSolarId = parqueId
        });
    }

    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancellationToken)
    {
        var nodo = await _nodoService.ObtenerPorIdAsync(id, cancellationToken);
        if (nodo is null)
        {
            return NotFound();
        }

        var paneles = await _panelService.ObtenerPorNodoAsync(id, null, cancellationToken);

        return View(new NodoDetalleViewModel
        {
            Nodo = nodo,
            Paneles = paneles
        });
    }

    public async Task<IActionResult> Crear(Guid? parqueId, CancellationToken cancellationToken)
    {
        var formulario = new NodoFormViewModel { ParqueSolarId = parqueId ?? Guid.Empty };
        await CargarParquesAsync(formulario, cancellationToken);

        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(NodoFormViewModel formulario, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await CargarParquesAsync(formulario, cancellationToken);
            return View(formulario);
        }

        try
        {
            var request = new CrearNodoRequest(
                formulario.Nombre,
                formulario.UbicacionDentroParque,
                formulario.ParqueSolarId,
                ArmarInversorRequest(formulario),
                ArmarSensorRequest(formulario));

            var nodoId = await _nodoService.CrearAsync(request, cancellationToken);

            TempData["MensajeExito"] = $"El nodo '{formulario.Nombre}' se creó correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = nodoId });
        }
        catch (ReglaDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarParquesAsync(formulario, cancellationToken);
            return View(formulario);
        }
    }

    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        var nodo = await _nodoService.ObtenerPorIdAsync(id, cancellationToken);
        if (nodo is null)
        {
            return NotFound();
        }

        var formulario = new NodoFormViewModel
        {
            Id = nodo.Id,
            Nombre = nodo.Nombre,
            UbicacionDentroParque = nodo.UbicacionDentroParque,
            ParqueSolarId = nodo.ParqueSolarId,
            InversorModelo = nodo.Inversor.Modelo,
            InversorNumeroSerie = nodo.Inversor.NumeroSerie,
            InversorEficienciaConversion = nodo.Inversor.EficienciaConversion,
            InversorVolcadoRedActivo = nodo.Inversor.VolcadoRedActivo,
            IdentificadorHardwareARM = nodo.Inversor.Sensor?.IdentificadorHardwareARM ?? string.Empty,
            SensorVersionFirmware = nodo.Inversor.Sensor?.VersionFirmware ?? string.Empty,
            SensorUrlBase = nodo.Inversor.Sensor?.UrlBase ?? "http://localhost:5100",
            SensorActivo = nodo.Inversor.Sensor?.Activo ?? true
        };

        await CargarParquesAsync(formulario, cancellationToken);
        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, NodoFormViewModel formulario, CancellationToken cancellationToken)
    {
        if (id != formulario.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await CargarParquesAsync(formulario, cancellationToken);
            return View(formulario);
        }

        try
        {
            var request = new ActualizarNodoRequest(
                id,
                formulario.Nombre,
                formulario.UbicacionDentroParque,
                formulario.ParqueSolarId,
                ArmarInversorRequest(formulario),
                ArmarSensorRequest(formulario));

            var actualizado = await _nodoService.ActualizarAsync(request, cancellationToken);
            if (!actualizado)
            {
                return NotFound();
            }

            TempData["MensajeExito"] = $"El nodo '{formulario.Nombre}' se actualizó correctamente.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (ReglaDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarParquesAsync(formulario, cancellationToken);
            return View(formulario);
        }
    }

    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        var nodo = await _nodoService.ObtenerPorIdAsync(id, cancellationToken);
        if (nodo is null)
        {
            return NotFound();
        }

        return View(nodo);
    }

    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var eliminado = await _nodoService.EliminarAsync(id, cancellationToken);
            if (!eliminado)
            {
                return NotFound();
            }

            TempData["MensajeExito"] = "El nodo se eliminó correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (ReglaDeNegocioException ex)
        {
            TempData["MensajeError"] = ex.Message;
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }

    private static InversorRequest ArmarInversorRequest(NodoFormViewModel formulario)
        => new(
            formulario.InversorModelo,
            formulario.InversorNumeroSerie,
            formulario.InversorEficienciaConversion,
            formulario.InversorVolcadoRedActivo);

    private static SensorDispositivoRequest ArmarSensorRequest(NodoFormViewModel formulario)
        => new(
            formulario.IdentificadorHardwareARM,
            formulario.SensorVersionFirmware,
            formulario.SensorUrlBase,
            formulario.SensorActivo);

    private async Task CargarParquesAsync(NodoFormViewModel formulario, CancellationToken cancellationToken)
        => formulario.Parques = await _parqueService.ObtenerTodosAsync(cancellationToken);
}
