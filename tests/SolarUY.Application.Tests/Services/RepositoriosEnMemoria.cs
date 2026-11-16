using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Repositorio de telemetría falso: devuelve lo que se le cargó y anota con qué parámetros lo
/// consultaron. Lo que una prueba no necesita falla si alguien lo llama, para detectar usos inesperados.
/// </summary>
internal sealed class TelemetriaRepositoryEnMemoria : ITelemetriaRepository
{
    public IReadOnlyList<AgregadoPotencia> Agregados { get; init; } = [];
    public IReadOnlyList<LecturaTelemetria> Lecturas { get; init; } = [];

    public Guid? UltimoPanelId { get; private set; }
    public Guid? UltimoParqueId { get; private set; }
    public GranularidadConsulta? UltimaGranularidad { get; private set; }
    public DateTime? UltimoDesde { get; private set; }
    public DateTime? UltimoHasta { get; private set; }
    public DateTime? DesdeUtcPedido { get; private set; }

    public Task<IReadOnlyList<AgregadoPotencia>> ObtenerAgregadoPorPanelAsync(Guid panelSolarId,
        GranularidadConsulta granularidad, DateTime? desde = null, DateTime? hasta = null,
        CancellationToken cancellationToken = default)
    {
        UltimoPanelId = panelSolarId;
        UltimaGranularidad = granularidad;
        UltimoDesde = desde;
        UltimoHasta = hasta;
        return Task.FromResult(Agregados);
    }

    public Task<IReadOnlyList<AgregadoPotencia>> ObtenerAgregadoGeneralAsync(Guid? parqueSolarId,
        GranularidadConsulta granularidad, DateTime? desde = null, DateTime? hasta = null,
        CancellationToken cancellationToken = default)
    {
        UltimoParqueId = parqueSolarId;
        UltimaGranularidad = granularidad;
        UltimoDesde = desde;
        UltimoHasta = hasta;
        return Task.FromResult(Agregados);
    }

    public Task<IReadOnlyList<LecturaTelemetria>> ObtenerDesdeAsync(DateTime desdeUtc,
        CancellationToken cancellationToken = default)
    {
        DesdeUtcPedido = desdeUtc;
        return Task.FromResult(Lecturas);
    }

    public Task AgregarAsync(LecturaTelemetria lectura, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task AgregarVariasAsync(IReadOnlyList<LecturaTelemetria> lecturas,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<LecturaTelemetria>> ObtenerPorPanelAsync(Guid panelSolarId, DateTime desde,
        DateTime hasta, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public int? UltimaCantidadPedida { get; private set; }

    public Task<IReadOnlyList<LecturaTelemetria>> ObtenerUltimasPorPanelAsync(Guid panelSolarId, int cantidad,
        CancellationToken cancellationToken = default)
    {
        UltimoPanelId = panelSolarId;
        UltimaCantidadPedida = cantidad;
        return Task.FromResult(Lecturas);
    }

    public Task<int> EliminarAnterioresAsync(DateTime limiteUtc, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

/// <summary>Repositorio de parques falso: solo sirve la búsqueda por id y el listado completo.</summary>
internal sealed class ParqueRepositoryEnMemoria(params ParqueSolar[] parques) : IParqueRepository
{
    public Task<IReadOnlyList<ParqueSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ParqueSolar>>(parques.ToList());

    public Task<ParqueSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(parques.FirstOrDefault(parque => parque.Id == id));

    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task AgregarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task ActualizarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task EliminarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
