using SolarUY.Domain.Entities;

namespace SolarUY.Domain.Repositories;

/// <summary>
/// Contrato de persistencia del agregado <see cref="PanelSolar"/>.
/// </summary>
public interface IPanelRepository
{
    /// <summary>Devuelve todos los paneles (sin tracking).</summary>
    Task<IReadOnlyList<PanelSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Código y modelo de cada panel ("P01 (JAM72S30-550/MR)"), para pantallas que necesitan
    /// mostrar a qué panel pertenece algo (como Mantenimiento) sin cargar el panel completo.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ObtenerDescripcionesAsync(CancellationToken cancellationToken = default);

    /// <summary>Devuelve los paneles de un nodo, opcionalmente filtrados por estado de salud (sin tracking).</summary>
    Task<IReadOnlyList<PanelSolar>> ObtenerPorNodoAsync(Guid nodoId,
        Enums.EstadoSalud? estado = null, CancellationToken cancellationToken = default);

    /// <summary>Devuelve el panel, o <c>null</c> si no existe.</summary>
    Task<PanelSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si ya hay un panel con ese código dentro del nodo. El código es el vínculo con el
    /// hardware, así que no puede repetirse dentro de un mismo nodo.
    /// </summary>
    /// <param name="panelExcluidoId">Panel que se debe ignorar, para el caso de una edición.</param>
    Task<bool> ExisteCodigoEnNodoAsync(Guid nodoId, string codigoPanel, Guid? panelExcluidoId = null,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(PanelSolar panel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste los cambios del panel. El agregado ya está siendo trackeado por el contexto,
    /// porque se obtuvo con <see cref="ObtenerPorIdAsync"/>.
    /// </summary>
    Task ActualizarAsync(PanelSolar panel, CancellationToken cancellationToken = default);

    Task EliminarAsync(PanelSolar panel, CancellationToken cancellationToken = default);
}
