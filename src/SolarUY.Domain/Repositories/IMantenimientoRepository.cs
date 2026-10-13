using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;

namespace SolarUY.Domain.Repositories;

/// <summary>
/// Contrato de persistencia de las tareas de mantenimiento.
/// </summary>
public interface IMantenimientoRepository
{
    Task<IReadOnlyList<TareaMantenimiento>> ObtenerTodosAsync(EstadoMantenimiento? estado = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TareaMantenimiento>> ObtenerPorPanelAsync(Guid panelSolarId,
        CancellationToken cancellationToken = default);

    Task<TareaMantenimiento?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AgregarAsync(TareaMantenimiento tarea, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste los cambios de la tarea. La entidad ya está siendo trackeada por el contexto,
    /// porque se obtuvo con <see cref="ObtenerPorIdAsync"/>.
    /// </summary>
    Task ActualizarAsync(TareaMantenimiento tarea, CancellationToken cancellationToken = default);
}
