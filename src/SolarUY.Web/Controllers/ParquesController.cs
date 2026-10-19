using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Exceptions;
using SolarUY.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace SolarUY.Web.Controllers;

/// <summary>
/// ABM de parques solares (specs.md §5.1).
/// </summary>
// Solo los administradores (rol de Keycloak) pueden gestionar parques.
[Authorize(Roles = "Administrador")]
public class ParquesController : Controller
{
    private readonly ParqueService _parqueService;
    private readonly NodoService _nodoService;

    public ParquesController(ParqueService parqueService, NodoService nodoService)
    {
        _parqueService = parqueService;
        _nodoService = nodoService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var parques = await _parqueService.ObtenerTodosAsync(cancellationToken);
        return View(parques);
    }

    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancellationToken)
    {
        var parque = await _parqueService.ObtenerPorIdAsync(id, cancellationToken);
        if (parque is null)
        {
            return NotFound();
        }

        var nodos = await _nodoService.ObtenerPorParqueAsync(id, cancellationToken);

        return View(new ParqueDetalleViewModel
        {
            Parque = parque,
            Nodos = nodos
        });
    }

    public IActionResult Crear() => View(new ParqueFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ParqueFormViewModel formulario, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        try
        {
            var request = new CrearParqueRequest(
                formulario.Nombre,
                formulario.Ubicacion,
                formulario.CapacidadInstaladaKW,
                formulario.FechaInstalacion);

            await _parqueService.CrearAsync(request, cancellationToken);

            TempData["MensajeExito"] = $"El parque '{formulario.Nombre}' se creó correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (ReglaDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }
    }

    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        var parque = await _parqueService.ObtenerPorIdAsync(id, cancellationToken);
        if (parque is null)
        {
            return NotFound();
        }

        return View(new ParqueFormViewModel
        {
            Id = parque.Id,
            Nombre = parque.Nombre,
            Ubicacion = parque.Ubicacion,
            CapacidadInstaladaKW = parque.CapacidadInstaladaKW,
            FechaInstalacion = parque.FechaInstalacion
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, ParqueFormViewModel formulario, CancellationToken cancellationToken)
    {
        if (id != formulario.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        try
        {
            var request = new ActualizarParqueRequest(
                id,
                formulario.Nombre,
                formulario.Ubicacion,
                formulario.CapacidadInstaladaKW,
                formulario.FechaInstalacion);

            var actualizado = await _parqueService.ActualizarAsync(request, cancellationToken);
            if (!actualizado)
            {
                return NotFound();
            }

            TempData["MensajeExito"] = $"El parque '{formulario.Nombre}' se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (ReglaDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }
    }

    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        var parque = await _parqueService.ObtenerPorIdAsync(id, cancellationToken);
        if (parque is null)
        {
            return NotFound();
        }

        return View(parque);
    }

    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var eliminado = await _parqueService.EliminarAsync(id, cancellationToken);
            if (!eliminado)
            {
                return NotFound();
            }

            TempData["MensajeExito"] = "El parque se eliminó correctamente.";
        }
        catch (ReglaDeNegocioException ex)
        {
            TempData["MensajeError"] = ex.Message;
            return RedirectToAction(nameof(Detalle), new { id });
        }

        return RedirectToAction(nameof(Index));
    }
}
