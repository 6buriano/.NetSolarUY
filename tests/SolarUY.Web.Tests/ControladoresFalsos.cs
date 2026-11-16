using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using SolarUY.Application.Sensors;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Web.Tests;

/// <summary>
/// Armado de entidades y de los objetos que necesita un controlador para correr sin servidor web
/// (TempData, configuración, entorno). Los valores por defecto cumplen las reglas del dominio.
/// </summary>
internal static class FixtureWeb
{
    public static readonly DateTime Instalacion = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    public static ParqueSolar NuevoParque(string nombre = "Parque Norte")
        => new(nombre, "Salto", 12.5, Instalacion);

    public static Nodo NuevoNodo(Guid? parqueId = null, string nombre = "Nodo A", string numeroSerie = "SN-001",
        string? hardware = null)
    {
        var inversor = new Inversor("Huawei SUN2000", numeroSerie, 98.5, true);
        var nodo = new Nodo(nombre, "Sector 1", parqueId ?? Guid.NewGuid(), inversor);

        if (hardware is not null)
        {
            inversor.RegistrarSensor(new SensorDispositivo(hardware, "1.0.0", true, "http://sensor-a:5100"));
        }

        return nodo;
    }

    public static PanelSolar NuevoPanel(Guid nodoId, string codigo = "P01", double potenciaW = 550d,
        EstadoSalud estado = EstadoSalud.OPERATIVO)
        => new(codigo, "JAM72S30", "JA Solar", potenciaW, estado, Instalacion, nodoId);

    /// <summary>TempData en memoria: no hay sesión ni cookie, solo el diccionario.</summary>
    public static TempDataDictionary NuevoTempData()
        => new(new DefaultHttpContext(), new TempDataProviderVacio());

    public static IConfiguration Configuracion(string? urlSimulador = null)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(urlSimulador is null
                ? []
                : new Dictionary<string, string?> { ["Simulador:UrlBase"] = urlSimulador })
            .Build();
}

internal sealed class TempDataProviderVacio : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
    }
}

/// <summary>Entorno web falso: solo importa el nombre (Development o Production).</summary>
internal sealed class EntornoWebFalso(string nombre) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "SolarUY.Web";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string WebRootPath { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = nombre;
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}

/// <summary>Cliente de sensores falso: responde siempre lo mismo y anota cada orden que recibe.</summary>
internal sealed class SensorClientFalso(ResultadoOrdenSensor resultado = ResultadoOrdenSensor.Ok) : ISensorClient
{
    public List<string> Ordenes { get; } = [];

    public Task<EstadoSensorDto?> ObtenerEstadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<ResultadoOrdenSensor> ActivarPanelAsync(string urlBase, string identificadorHardwareARM,
        string codigoPanel, CancellationToken cancellationToken = default)
        => Registrar($"activar-panel {urlBase} {identificadorHardwareARM} {codigoPanel}");

    public Task<ResultadoOrdenSensor> DesactivarPanelAsync(string urlBase, string identificadorHardwareARM,
        string codigoPanel, CancellationToken cancellationToken = default)
        => Registrar($"desactivar-panel {urlBase} {identificadorHardwareARM} {codigoPanel}");

    public Task<ResultadoOrdenSensor> ActivarVolcadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
        => Registrar($"activar-volcado {urlBase} {identificadorHardwareARM}");

    public Task<ResultadoOrdenSensor> DesactivarVolcadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
        => Registrar($"desactivar-volcado {urlBase} {identificadorHardwareARM}");

    private Task<ResultadoOrdenSensor> Registrar(string orden)
    {
        Ordenes.Add(orden);
        return Task.FromResult(resultado);
    }
}

/// <summary>Servicio de disponibilidad de sensores falso: devuelve lo que se configure.</summary>
internal sealed class SensorDisponibilidadFalso : ISensorDisponibilidadService
{
    public DisponibilidadSensor Disponibilidad { get; init; } = new(true, true, true, []);
    public ResultadoAltaSensor Alta { get; init; } = new(true, null, []);

    public List<string> Verificados { get; } = [];
    public List<string> Creados { get; } = [];

    public Task<DisponibilidadSensor> VerificarAsync(string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
    {
        Verificados.Add(identificadorHardwareARM);
        return Task.FromResult(Disponibilidad);
    }

    public Task<ResultadoAltaSensor> CrearAsync(string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
    {
        Creados.Add(identificadorHardwareARM);
        return Task.FromResult(Alta);
    }
}

/// <summary>Repositorios de parques, nodos y paneles que guardan en memoria y anotan lo que se les pidió.</summary>
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
        _parques.Add(parque);
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
        _parques.Remove(parque);
        return Task.CompletedTask;
    }
}

internal sealed class NodoRepositoryAbmFalso(params Nodo[] nodosIniciales) : INodoRepository
{
    private readonly List<Nodo> _nodos = [.. nodosIniciales];

    public bool NumeroSerieOcupado { get; init; }
    public bool IdentificadorOcupado { get; init; }

    public List<Nodo> Agregados { get; } = [];
    public List<Nodo> Actualizados { get; } = [];
    public List<Nodo> Eliminados { get; } = [];

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
        => Task.FromResult(NumeroSerieOcupado);

    public Task<bool> ExisteIdentificadorHardwareAsync(string identificadorHardwareARM, Guid? nodoExcluidoId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(IdentificadorOcupado);

    public Task AgregarAsync(Nodo nodo, CancellationToken cancellationToken = default)
    {
        Agregados.Add(nodo);
        _nodos.Add(nodo);
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Nodo nodo, SensorDispositivo? sensorNuevo = null,
        CancellationToken cancellationToken = default)
    {
        Actualizados.Add(nodo);
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Nodo nodo, CancellationToken cancellationToken = default)
    {
        Eliminados.Add(nodo);
        _nodos.Remove(nodo);
        return Task.CompletedTask;
    }
}

internal sealed class PanelRepositoryAbmFalso(params PanelSolar[] panelesIniciales) : IPanelRepository
{
    private readonly List<PanelSolar> _paneles = [.. panelesIniciales];

    public bool CodigoOcupado { get; init; }

    public List<PanelSolar> Agregados { get; } = [];
    public List<PanelSolar> Actualizados { get; } = [];
    public List<PanelSolar> Eliminados { get; } = [];

    public Task<IReadOnlyList<PanelSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PanelSolar>>(_paneles.ToList());

    public Task<IReadOnlyDictionary<Guid, string>> ObtenerDescripcionesAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, string>>(_paneles.ToDictionary(panel => panel.Id,
            panel => $"{panel.CodigoPanel} ({panel.Modelo})"));

    public Task<IReadOnlyList<PanelSolar>> ObtenerPorNodoAsync(Guid nodoId, EstadoSalud? estado = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PanelSolar>>(_paneles
            .Where(panel => panel.NodoId == nodoId && (estado is null || panel.Estado == estado))
            .ToList());

    public Task<PanelSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_paneles.FirstOrDefault(panel => panel.Id == id));

    public Task<bool> ExisteCodigoEnNodoAsync(Guid nodoId, string codigoPanel, Guid? panelExcluidoId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(CodigoOcupado);

    public Task AgregarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
    {
        Agregados.Add(panel);
        _paneles.Add(panel);
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
        _paneles.Remove(panel);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Telemetría falsa que sirve las últimas lecturas y los agregados que se le carguen, y recuerda con qué
/// parámetros se los pidieron. Lo demás falla si alguien lo llama.
/// </summary>
internal sealed class TelemetriaRepositoryDatosFalso : ITelemetriaRepository
{
    public IReadOnlyList<LecturaTelemetria> Ultimas { get; init; } = [];
    public IReadOnlyList<AgregadoPotencia> Agregados { get; init; } = [];

    public Guid? UltimoPanelId { get; private set; }
    public GranularidadConsulta? UltimaGranularidad { get; private set; }
    public DateTime? UltimoDesde { get; private set; }
    public DateTime? UltimoHasta { get; private set; }

    public Task<IReadOnlyList<LecturaTelemetria>> ObtenerUltimasPorPanelAsync(Guid panelSolarId, int cantidad,
        CancellationToken cancellationToken = default)
    {
        UltimoPanelId = panelSolarId;
        return Task.FromResult(Ultimas);
    }

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

    public Task AgregarAsync(LecturaTelemetria lectura, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task AgregarVariasAsync(IReadOnlyList<LecturaTelemetria> lecturas,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<LecturaTelemetria>> ObtenerPorPanelAsync(Guid panelSolarId, DateTime desde,
        DateTime hasta, CancellationToken cancellationToken = default)
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
