using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Armado de entidades y pedidos válidos para las pruebas de los servicios de alta, baja y modificación
/// (nodos, paneles y parques). Los valores por defecto cumplen todas las reglas del dominio.
/// </summary>
internal static class FixtureAbm
{
    public static readonly DateTime Instalacion = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    public static ParqueSolar NuevoParque(string nombre = "Parque Norte")
        => new(nombre, "Salto", 12.5, Instalacion);

    /// <summary>Crea un nodo con su inversor y, si se indica un identificador de hardware, con su sensor.</summary>
    public static Nodo NuevoNodo(Guid? parqueId = null, string nombre = "Nodo A", string numeroSerie = "SN-001",
        string? hardware = null, bool sensorActivo = true)
    {
        var inversor = new Inversor("Huawei SUN2000", numeroSerie, 98.5, true);
        var nodo = new Nodo(nombre, "Sector 1", parqueId ?? Guid.NewGuid(), inversor);

        if (hardware is not null)
        {
            inversor.RegistrarSensor(new SensorDispositivo(hardware, "1.0.0", sensorActivo, "http://localhost:5100"));
        }

        return nodo;
    }

    public static PanelSolar NuevoPanel(Guid nodoId, string codigo = "P01", double potenciaW = 550d,
        EstadoSalud estado = EstadoSalud.OPERATIVO, bool activo = true)
        => new(codigo, "JAM72S30", "JA Solar", potenciaW, estado, Instalacion, nodoId, activo);
}

/// <summary>
/// Repositorio de parques falso para el alta, baja y modificación: guarda en memoria y anota qué se le pidió.
/// </summary>
internal sealed class ParqueRepositoryAbmFalso(params ParqueSolar[] parquesIniciales) : IParqueRepository
{
    private readonly List<ParqueSolar> _parques = [.. parquesIniciales];

    public List<ParqueSolar> Agregados { get; } = [];
    public List<ParqueSolar> Actualizados { get; } = [];
    public List<ParqueSolar> Eliminados { get; } = [];

    public Task<IReadOnlyList<ParqueSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ParqueSolar>>(_parques.ToList());

    public Task<ParqueSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_parques.FirstOrDefault(parque => parque.Id == id));

    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_parques.Any(parque => parque.Id == id));

    public Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, string>>(_parques.ToDictionary(parque => parque.Id,
            parque => parque.Nombre));

    public Task AgregarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
    {
        Agregados.Add(parque);
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
    {
        Actualizados.Add(parque);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
    {
        Eliminados.Add(parque);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Repositorio de nodos falso. Las dos preguntas de unicidad (número de serie e identificador de hardware)
/// responden lo que se configure y recuerdan qué nodo se pidió ignorar, para comprobar la edición.
/// </summary>
internal sealed class NodoRepositoryAbmFalso(params Nodo[] nodosIniciales) : INodoRepository
{
    private readonly List<Nodo> _nodos = [.. nodosIniciales];

    public bool NumeroSerieOcupado { get; init; }
    public bool IdentificadorOcupado { get; init; }

    public Guid? NodoExcluidoEnNumeroSerie { get; private set; }
    public Guid? NodoExcluidoEnIdentificador { get; private set; }
    public bool SePreguntoPorIdentificador { get; private set; }

    public List<Nodo> Agregados { get; } = [];
    public List<Nodo> Actualizados { get; } = [];
    public List<Nodo> Eliminados { get; } = [];
    public SensorDispositivo? SensorNuevoInformado { get; private set; }

    public Task<IReadOnlyList<Nodo>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Nodo>>(_nodos.ToList());

    public Task<IReadOnlyList<Nodo>> ObtenerPorParqueAsync(Guid parqueSolarId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Nodo>>(_nodos.Where(nodo => nodo.ParqueSolarId == parqueSolarId).ToList());

    public Task<Nodo?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_nodos.FirstOrDefault(nodo => nodo.Id == id));

    public Task<IReadOnlyList<Nodo>> ObtenerConSensoresActivosAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_nodos.Any(nodo => nodo.Id == id));

    public Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, string>>(_nodos.ToDictionary(nodo => nodo.Id,
            nodo => nodo.Nombre));

    public Task<bool> ExisteNumeroSerieInversorAsync(string numeroSerie, Guid? nodoExcluidoId = null,
        CancellationToken cancellationToken = default)
    {
        NodoExcluidoEnNumeroSerie = nodoExcluidoId;
        return Task.FromResult(NumeroSerieOcupado);
    }

    public Task<bool> ExisteIdentificadorHardwareAsync(string identificadorHardwareARM, Guid? nodoExcluidoId = null,
        CancellationToken cancellationToken = default)
    {
        SePreguntoPorIdentificador = true;
        NodoExcluidoEnIdentificador = nodoExcluidoId;
        return Task.FromResult(IdentificadorOcupado);
    }

    public Task AgregarAsync(Nodo nodo, CancellationToken cancellationToken = default)
    {
        Agregados.Add(nodo);
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Nodo nodo, SensorDispositivo? sensorNuevo = null,
        CancellationToken cancellationToken = default)
    {
        Actualizados.Add(nodo);
        SensorNuevoInformado = sensorNuevo;
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Nodo nodo, CancellationToken cancellationToken = default)
    {
        Eliminados.Add(nodo);
        return Task.CompletedTask;
    }
}

/// <summary>Repositorio de paneles falso para el alta, baja y modificación.</summary>
internal sealed class PanelRepositoryAbmFalso(params PanelSolar[] panelesIniciales) : IPanelRepository
{
    private readonly List<PanelSolar> _paneles = [.. panelesIniciales];

    public bool CodigoOcupado { get; init; }
    public Guid? PanelExcluidoEnCodigo { get; private set; }

    public List<PanelSolar> Agregados { get; } = [];
    public List<PanelSolar> Actualizados { get; } = [];
    public List<PanelSolar> Eliminados { get; } = [];

    public Task<IReadOnlyList<PanelSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PanelSolar>>(_paneles.ToList());

    public Task<IReadOnlyDictionary<Guid, string>> ObtenerDescripcionesAsync(
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<PanelSolar>> ObtenerPorNodoAsync(Guid nodoId, EstadoSalud? estado = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PanelSolar>>(_paneles
            .Where(panel => panel.NodoId == nodoId && (estado is null || panel.Estado == estado))
            .ToList());

    public Task<PanelSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_paneles.FirstOrDefault(panel => panel.Id == id));

    public Task<bool> ExisteCodigoEnNodoAsync(Guid nodoId, string codigoPanel, Guid? panelExcluidoId = null,
        CancellationToken cancellationToken = default)
    {
        PanelExcluidoEnCodigo = panelExcluidoId;
        return Task.FromResult(CodigoOcupado);
    }

    public Task AgregarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
    {
        Agregados.Add(panel);
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
    {
        Actualizados.Add(panel);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
    {
        Eliminados.Add(panel);
        return Task.CompletedTask;
    }
}
