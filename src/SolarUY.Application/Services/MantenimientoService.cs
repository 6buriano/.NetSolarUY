using SolarUY.Application.DTOs;
using SolarUY.Application.Mappings;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Services;

/// <summary>
/// Casos de uso de las tareas de mantenimiento: se programan sobre un panel y van pasando por
/// PENDIENTE → EN_PROCESO → COMPLETADO (o se cancelan), igual que define el dominio.
/// </summary>
public class MantenimientoService
{
    private readonly IMantenimientoRepository _mantenimientoRepository;
    private readonly IPanelRepository _panelRepository;
    private readonly TimeProvider _reloj;

    public MantenimientoService(IMantenimientoRepository mantenimientoRepository, IPanelRepository panelRepository,
        TimeProvider reloj)
    {
        _mantenimientoRepository = mantenimientoRepository;
        _panelRepository = panelRepository;
        _reloj = reloj;
    }

    public async Task<IReadOnlyList<MantenimientoDto>> ObtenerTodosAsync(EstadoMantenimiento? estado = null,
        CancellationToken cancellationToken = default)
    {
        var tareas = await _mantenimientoRepository.ObtenerTodosAsync(estado, cancellationToken);
        var descripciones = await _panelRepository.ObtenerDescripcionesAsync(cancellationToken);

        return tareas
            .Select(tarea => tarea.ToDto(descripciones.GetValueOrDefault(tarea.PanelSolarId)))
            .ToList();
    }

    public async Task<IReadOnlyList<MantenimientoDto>> ObtenerPorPanelAsync(Guid panelSolarId,
        CancellationToken cancellationToken = default)
    {
        var tareas = await _mantenimientoRepository.ObtenerPorPanelAsync(panelSolarId, cancellationToken);
        var descripciones = await _panelRepository.ObtenerDescripcionesAsync(cancellationToken);

        return tareas
            .Select(tarea => tarea.ToDto(descripciones.GetValueOrDefault(tarea.PanelSolarId)))
            .ToList();
    }

    public async Task<MantenimientoDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tarea = await _mantenimientoRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (tarea is null)
        {
            return null;
        }

        var descripciones = await _panelRepository.ObtenerDescripcionesAsync(cancellationToken);
        return tarea.ToDto(descripciones.GetValueOrDefault(tarea.PanelSolarId));
    }

    public async Task<Guid> CrearAsync(CrearMantenimientoRequest request, CancellationToken cancellationToken = default)
    {
        var panel = await _panelRepository.ObtenerPorIdAsync(request.PanelSolarId, cancellationToken);
        if (panel is null)
        {
            throw new ReglaDeNegocioException("El panel seleccionado no existe.");
        }

        var tarea = new TareaMantenimiento(request.PanelSolarId, request.Tipo, request.FechaProgramada,
            request.Descripcion, request.PersonalAsignado);

        await _mantenimientoRepository.AgregarAsync(tarea, cancellationToken);
        return tarea.Id;
    }

    public async Task<bool> ReprogramarAsync(ReprogramarMantenimientoRequest request,
        CancellationToken cancellationToken = default)
    {
        var tarea = await _mantenimientoRepository.ObtenerPorIdAsync(request.Id, cancellationToken);
        if (tarea is null)
        {
            return false;
        }

        tarea.Reprogramar(request.FechaProgramada, request.Descripcion, request.PersonalAsignado);
        await _mantenimientoRepository.ActualizarAsync(tarea, cancellationToken);
        return true;
    }

    public async Task<bool> IniciarAsync(Guid id, CancellationToken cancellationToken = default)
        => await CambiarEstadoAsync(id, tarea => tarea.Iniciar(), cancellationToken);

    /// <summary>
    /// Completa la tarea con la fecha en que se terminó el trabajo y las observaciones de cierre. La
    /// fecha la informa quien hizo el trabajo: la del servidor no coincide con la real si la marca
    /// más tarde.
    /// </summary>
    public async Task<bool> CompletarAsync(Guid id, DateTime fechaRealizada, string? observaciones,
        CancellationToken cancellationToken = default)
        => await CambiarEstadoAsync(id,
            tarea => tarea.Completar(fechaRealizada, observaciones, _reloj.GetUtcNow().UtcDateTime),
            cancellationToken);

    public async Task<bool> CancelarAsync(Guid id, CancellationToken cancellationToken = default)
        => await CambiarEstadoAsync(id, tarea => tarea.Cancelar(), cancellationToken);

    private async Task<bool> CambiarEstadoAsync(Guid id, Action<TareaMantenimiento> accion,
        CancellationToken cancellationToken)
    {
        var tarea = await _mantenimientoRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (tarea is null)
        {
            return false;
        }

        accion(tarea);
        await _mantenimientoRepository.ActualizarAsync(tarea, cancellationToken);
        return true;
    }
}
