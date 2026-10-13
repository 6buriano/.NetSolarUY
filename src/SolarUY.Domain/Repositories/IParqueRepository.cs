using SolarUY.Domain.Entities;

namespace SolarUY.Domain.Repositories;

/// <summary>
/// Contrato de persistencia del agregado <see cref="ParqueSolar"/>.
/// </summary>
public interface IParqueRepository
{
    /// <summary>Devuelve todos los parques con sus nodos y paneles cargados (sin tracking).</summary>
    Task<IReadOnlyList<ParqueSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve el parque con sus nodos y paneles, o <c>null</c> si no existe.
    /// Los paneles se incluyen porque los totales del DTO se calculan a partir de ellos.
    /// </summary>
    Task<ParqueSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Indica si existe un parque con ese identificador. Evita cargar el agregado completo.</summary>
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve sólo el identificador y el nombre de cada parque. Se usa para completar los
    /// nombres en otros listados y para armar los desplegables, sin cargar el grafo completo.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(CancellationToken cancellationToken = default);

    Task AgregarAsync(ParqueSolar parque, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste los cambios del parque. El agregado ya está siendo trackeado por el contexto,
    /// porque se obtuvo con <see cref="ObtenerPorIdAsync"/>.
    /// </summary>
    Task ActualizarAsync(ParqueSolar parque, CancellationToken cancellationToken = default);

    Task EliminarAsync(ParqueSolar parque, CancellationToken cancellationToken = default);
}
