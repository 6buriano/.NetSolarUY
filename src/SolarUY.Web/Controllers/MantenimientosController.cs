using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;
using SolarUY.Web.Models;

namespace SolarUY.Web.Controllers;

/// <summary>
/// Gestión de tareas de mantenimiento: se programan sobre un panel y van pasando por
/// PENDIENTE → EN_PROCESO → COMPLETADO (o se cancelan).
/// </summary>
[Authorize(Roles = "Administrador")]
public class MantenimientosController : Controller
{
    private readonly MantenimientoService _mantenimientoService;
    private readonly PanelService _panelService;

    public MantenimientosController(MantenimientoService mantenimientoService, PanelService panelService)
    {
        _mantenimientoService = mantenimientoService;
        _panelService = panelService;
    }

    public async Task<IActionResult> Index(Guid? panelId, EstadoMantenimiento? estado,
        CancellationToken cancellationToken)
    {
        var tareas = await _mantenimientoService.ObtenerTodosAsync(estado, cancellationToken);
        if (panelId is not null)
        {
            tareas = tareas.Where(tarea => tarea.PanelSolarId == panelId).ToList();
        }

        return View(new MantenimientosIndexViewModel
        {
            Tareas = tareas,
            Paneles = await _panelService.ObtenerTodosAsync(cancellationToken: cancellationToken),
            PanelSolarId = panelId,
            Estado = estado
        });
    }

    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancellationToken)
    {
        var tarea = await _mantenimientoService.ObtenerPorIdAsync(id, cancellationToken);
        if (tarea is null)
        {
            return NotFound();
        }

        return View(tarea);
    }

    public async Task<IActionResult> Crear(Guid? panelId, CancellationToken cancellationToken)
    {
        var formulario = new MantenimientoFormViewModel { PanelSolarId = panelId ?? Guid.Empty };
        await CargarPanelesAsync(formulario, cancellationToken);

        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(MantenimientoFormViewModel formulario, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await CargarPanelesAsync(formulario, cancellationToken);
            return View(formulario);
        }

        try
        {
            var request = new CrearMantenimientoRequest(
                formulario.PanelSolarId,
                formulario.Tipo,
                formulario.FechaProgramada,
                formulario.Descripcion,
                formulario.PersonalAsignado);

            var tareaId = await _mantenimientoService.CrearAsync(request, cancellationToken);

            TempData["MensajeExito"] = "La tarea de mantenimiento se programó correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = tareaId });
        }
        catch (ReglaDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarPanelesAsync(formulario, cancellationToken);
            return View(formulario);
        }
    }

    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        var tarea = await _mantenimientoService.ObtenerPorIdAsync(id, cancellationToken);
        if (tarea is null)
        {
            return NotFound();
        }

        if (tarea.Estado == EstadoMantenimiento.COMPLETADO)
        {
            TempData["MensajeError"] = "Una tarea completada no se puede reprogramar.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var formulario = new MantenimientoFormViewModel
        {
            Id = tarea.Id,
            PanelSolarId = tarea.PanelSolarId,
            Tipo = tarea.Tipo,
            FechaProgramada = tarea.FechaProgramada,
            Descripcion = tarea.Descripcion,
            PersonalAsignado = tarea.PersonalAsignado
        };

        await CargarPanelesAsync(formulario, cancellationToken);
        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, MantenimientoFormViewModel formulario,
        CancellationToken cancellationToken)
    {
        if (id != formulario.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await CargarPanelesAsync(formulario, cancellationToken);
            return View(formulario);
        }

        try
        {
            var request = new ReprogramarMantenimientoRequest(
                id,
                formulario.FechaProgramada,
                formulario.Descripcion,
                formulario.PersonalAsignado);

            var actualizado = await _mantenimientoService.ReprogramarAsync(request, cancellationToken);
            if (!actualizado)
            {
                return NotFound();
            }

            TempData["MensajeExito"] = "La tarea se reprogramó correctamente.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (ReglaDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarPanelesAsync(formulario, cancellationToken);
            return View(formulario);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Iniciar(Guid id, CancellationToken cancellationToken)
        => CambiarEstadoAsync(id, () => _mantenimientoService.IniciarAsync(id, cancellationToken),
            "La tarea pasó a estar en proceso.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Completar(Guid id, CancellationToken cancellationToken)
        => CambiarEstadoAsync(id, () => _mantenimientoService.CompletarAsync(id, cancellationToken),
            "La tarea se marcó como completada.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
        => CambiarEstadoAsync(id, () => _mantenimientoService.CancelarAsync(id, cancellationToken),
            "La tarea se canceló.");

    private async Task<IActionResult> CambiarEstadoAsync(Guid id, Func<Task<bool>> accion, string mensajeExito)
    {
        try
        {
            var aplicado = await accion();
            if (!aplicado)
            {
                return NotFound();
            }

            TempData["MensajeExito"] = mensajeExito;
        }
        catch (ReglaDeNegocioException ex)
        {
            TempData["MensajeError"] = ex.Message;
        }

        return RedirectToAction(nameof(Detalle), new { id });
    }

    private async Task CargarPanelesAsync(MantenimientoFormViewModel formulario, CancellationToken cancellationToken)
        => formulario.Paneles = await _panelService.ObtenerTodosAsync(cancellationToken: cancellationToken);
}
