using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Web.Tests;

/// <summary>Repositorio de mantenimientos en memoria que anota cuántas veces se guardó o actualizó algo.</summary>
internal sealed class MantenimientoRepositoryFalso(params TareaMantenimiento[] tareasIniciales)
    : IMantenimientoRepository
{
    private readonly List<TareaMantenimiento> _tareas = [.. tareasIniciales];

    public List<TareaMantenimiento> Agregadas { get; } = [];
    public int Actualizaciones { get; private set; }
    public EstadoMantenimiento? UltimoEstadoPedido { get; private set; }

    public Task<IReadOnlyList<TareaMantenimiento>> ObtenerTodosAsync(EstadoMantenimiento? estado = null,
        CancellationToken cancellationToken = default)
    {
        UltimoEstadoPedido = estado;
        return Task.FromResult<IReadOnlyList<TareaMantenimiento>>(_tareas.ToList());
    }

    public Task<IReadOnlyList<TareaMantenimiento>> ObtenerPorPanelAsync(Guid panelSolarId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TareaMantenimiento>>(
            _tareas.Where(tarea => tarea.PanelSolarId == panelSolarId).ToList());

    public Task<TareaMantenimiento?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_tareas.FirstOrDefault(tarea => tarea.Id == id));

    public Task AgregarAsync(TareaMantenimiento tarea, CancellationToken cancellationToken = default)
    {
        Agregadas.Add(tarea);
        _tareas.Add(tarea);
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(TareaMantenimiento tarea, CancellationToken cancellationToken = default)
    {
        Actualizaciones++;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Repositorio de paneles falso: sirve el listado, la búsqueda por id y las descripciones. El resto
/// falla si alguien lo llama, para detectar usos que la prueba no esperaba.
/// </summary>
internal sealed class PanelRepositoryFalso(params PanelSolar[] paneles) : IPanelRepository
{
    public Dictionary<Guid, string> Descripciones { get; } = [];

    public Task<IReadOnlyList<PanelSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PanelSolar>>(paneles.ToList());

    public Task<PanelSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(paneles.FirstOrDefault(panel => panel.Id == id));

    public Task<IReadOnlyDictionary<Guid, string>> ObtenerDescripcionesAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, string>>(Descripciones);

    public Task<IReadOnlyList<PanelSolar>> ObtenerPorNodoAsync(Guid nodoId, EstadoSalud? estado = null,
        CancellationToken cancellationToken = default)
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

/// <summary>Repositorio de telemetría falso: el listado de paneles no lo usa, así que todo falla si se llama.</summary>
internal sealed class TelemetriaRepositoryFalso : ITelemetriaRepository
{
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

    public Task<IReadOnlyList<AgregadoPotencia>> ObtenerAgregadoGeneralAsync(Guid? parqueSolarId,
        GranularidadConsulta granularidad, DateTime? desde = null, DateTime? hasta = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<LecturaTelemetria>> ObtenerDesdeAsync(DateTime desdeUtc,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<int> EliminarAnterioresAsync(DateTime limiteUtc, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
