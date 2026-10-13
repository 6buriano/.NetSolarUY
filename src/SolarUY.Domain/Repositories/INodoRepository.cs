using SolarUY.Domain.Entities;

namespace SolarUY.Domain.Repositories;

/// <summary>
/// Contrato de persistencia del agregado <see cref="Nodo"/>, incluyendo su <see cref="Inversor"/>.
/// </summary>
public interface INodoRepository
{
    /// <summary>Devuelve todos los nodos con su inversor y sus paneles cargados (sin tracking).</summary>
    Task<IReadOnlyList<Nodo>> ObtenerTodosAsync(CancellationToken cancellationToken = default);

    /// <summary>Devuelve los nodos de un parque con su inversor y sus paneles (sin tracking).</summary>
    Task<IReadOnlyList<Nodo>> ObtenerPorParqueAsync(Guid parqueSolarId, CancellationToken cancellationToken = default);

    /// <summary>Devuelve el nodo con su inversor y sus paneles, o <c>null</c> si no existe.</summary>
    Task<Nodo?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve los nodos que tienen un sensor ARM activo, con su inversor, su sensor y sus paneles.
    /// Es lo que necesita el colector para saber a quién consultarle la telemetría.
    /// </summary>
    Task<IReadOnlyList<Nodo>> ObtenerConSensoresActivosAsync(CancellationToken cancellationToken = default);

    /// <summary>Indica si existe un nodo con ese identificador. Evita cargar el agregado completo.</summary>
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve sólo el identificador y el nombre de cada nodo. Se usa para completar los
    /// nombres en otros listados y para armar los desplegables, sin cargar el grafo completo.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Controla la integridad del número de serie del inversor, que es único en todo el sistema.
    /// </summary>
    /// <param name="nodoExcluidoId">Nodo que se debe ignorar, para el caso de una edición.</param>
    Task<bool> ExisteNumeroSerieInversorAsync(string numeroSerie, Guid? nodoExcluidoId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Controla que el identificador de hardware del sensor no esté usado por otro nodo: es la
    /// dirección física del dispositivo.
    /// </summary>
    /// <param name="nodoExcluidoId">Nodo que se debe ignorar, para el caso de una edición.</param>
    Task<bool> ExisteIdentificadorHardwareAsync(string identificadorHardwareARM, Guid? nodoExcluidoId = null,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(Nodo nodo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste los cambios del nodo y de su inversor. El agregado ya está siendo trackeado
    /// por el contexto, porque se obtuvo con <see cref="ObtenerPorIdAsync"/>.
    /// </summary>
    Task ActualizarAsync(Nodo nodo, CancellationToken cancellationToken = default);

    Task EliminarAsync(Nodo nodo, CancellationToken cancellationToken = default);
}
