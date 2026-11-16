using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Infrastructure.Tests;

/// <summary>Repositorio de parques falso: solo sirve el listado completo y la búsqueda por id.</summary>
internal sealed class ParqueRepositoryFalso(params ParqueSolar[] parques) : IParqueRepository
{
    public Task<IReadOnlyList<ParqueSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ParqueSolar>>(parques.ToList());

    public Task<ParqueSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(parques.FirstOrDefault(parque => parque.Id == id));

    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task AgregarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task ActualizarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task EliminarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

/// <summary>Repositorio de paneles falso: solo sirve el listado completo.</summary>
internal sealed class PanelRepositoryFalso(IReadOnlyList<PanelSolar> paneles) : IPanelRepository
{
    public Task<IReadOnlyList<PanelSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(paneles);

    public Task<IReadOnlyDictionary<Guid, string>> ObtenerDescripcionesAsync(
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<PanelSolar>> ObtenerPorNodoAsync(Guid nodoId, EstadoSalud? estado = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PanelSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> ExisteCodigoEnNodoAsync(Guid nodoId, string codigoPanel, Guid? panelExcluidoId = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task AgregarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task ActualizarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task EliminarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

/// <summary>Repositorio de nodos falso: solo responde los nombres (los usa PanelService para armar el DTO).</summary>
internal sealed class NodoRepositoryFalso : INodoRepository
{
    public Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

    public Task<IReadOnlyList<Nodo>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<Nodo>> ObtenerPorParqueAsync(Guid parqueSolarId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Nodo?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<Nodo>> ObtenerConSensoresActivosAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> ExisteNumeroSerieInversorAsync(string numeroSerie, Guid? nodoExcluidoId = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> ExisteIdentificadorHardwareAsync(string identificadorHardwareARM, Guid? nodoExcluidoId = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task AgregarAsync(Nodo nodo, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task ActualizarAsync(Nodo nodo, SensorDispositivo? sensorNuevo = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task EliminarAsync(Nodo nodo, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

/// <summary>
/// Repositorio de telemetría falso: devuelve los agregados que se le cargaron y anota con qué rango
/// lo consultaron. Lo demás falla si alguien lo llama.
/// </summary>
internal sealed class TelemetriaRepositoryFalso : ITelemetriaRepository
{
    public IReadOnlyList<AgregadoPotencia> Agregados { get; init; } = [];

    public Guid? UltimoParqueId { get; private set; }
    public GranularidadConsulta? UltimaGranularidad { get; private set; }
    public DateTime? UltimoDesde { get; private set; }
    public DateTime? UltimoHasta { get; private set; }

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

    public Task AgregarAsync(LecturaTelemetria lectura, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task AgregarVariasAsync(IReadOnlyList<LecturaTelemetria> lecturas,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<LecturaTelemetria>> ObtenerPorPanelAsync(Guid panelSolarId, DateTime desde,
        DateTime hasta, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<LecturaTelemetria>> ObtenerUltimasPorPanelAsync(Guid panelSolarId, int cantidad,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<AgregadoPotencia>> ObtenerAgregadoPorPanelAsync(Guid panelSolarId,
        GranularidadConsulta granularidad, DateTime? desde = null, DateTime? hasta = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<LecturaTelemetria>> ObtenerDesdeAsync(DateTime desdeUtc,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<int> EliminarAnterioresAsync(DateTime limiteUtc, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

/// <summary>Reloj congelado.</summary>
internal sealed class RelojFijo(DateTimeOffset momento) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => momento;
}

/// <summary>Arma parques, nodos y paneles de prueba. Las colecciones del dominio se llenan agregando.</summary>
internal static class Fixture
{
    private static readonly DateTime FechaInstalacion = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    public static ParqueSolar Parque(string nombre = "Parque Norte")
        => new(nombre, "Ruta 5 km 20", 100, FechaInstalacion);

    public static Nodo Nodo(ParqueSolar parque, string nombre = "Nodo A")
    {
        var nodo = new Nodo(nombre, "Sector A", parque.Id,
            new Inversor("SUN2000-5KTL", $"SN-{Guid.NewGuid():N}"[..12], 0.97, false));

        parque.Nodos.Add(nodo);
        return nodo;
    }

    public static PanelSolar Panel(Nodo nodo, string codigo, EstadoSalud estado = EstadoSalud.OPERATIVO)
    {
        var panel = new PanelSolar(codigo, "CS6P-300", "Canadian Solar", 300, estado, FechaInstalacion, nodo.Id);
        nodo.Paneles.Add(panel);
        return panel;
    }
}
