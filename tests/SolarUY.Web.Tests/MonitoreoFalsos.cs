using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Web.Tests;

/// <summary>Arma el grafo mínimo parque → nodo → panel para las pruebas del monitoreo.</summary>
internal static class FixtureMonitoreo
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

    public static LecturaTelemetria Lectura(PanelSolar panel, DateTime fechaHora, double potenciaW = 250)
        => new(panel.Id, fechaHora, 8, 36, potenciaW, 45);
}

/// <summary>Repositorio de parques falso: solo sirve el listado completo.</summary>
internal sealed class ParqueRepositoryMonitoreoFalso(params ParqueSolar[] parques) : IParqueRepository
{
    public Task<IReadOnlyList<ParqueSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ParqueSolar>>(parques.ToList());

    public Task<ParqueSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

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

/// <summary>
/// Telemetría falsa para el monitoreo: cada consulta de "lecturas desde" le pregunta a una función qué
/// devolver, a la que le pasa el número de la consulta (1 la primera). Sirve para simular cambios entre
/// una vuelta del worker y la siguiente, o para que falle.
/// </summary>
internal sealed class TelemetriaRepositoryMonitoreoFalso(
    Func<int, IReadOnlyList<LecturaTelemetria>>? lecturasPorConsulta = null) : ITelemetriaRepository
{
    private int _consultas;

    public Task<IReadOnlyList<LecturaTelemetria>> ObtenerDesdeAsync(DateTime desdeUtc,
        CancellationToken cancellationToken = default)
    {
        var numero = Interlocked.Increment(ref _consultas);
        IReadOnlyList<LecturaTelemetria> lecturas = lecturasPorConsulta?.Invoke(numero) ?? [];
        return Task.FromResult(lecturas);
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

    public Task<IReadOnlyList<AgregadoPotencia>> ObtenerAgregadoGeneralAsync(Guid? parqueSolarId,
        GranularidadConsulta granularidad, DateTime? desde = null, DateTime? hasta = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<int> EliminarAnterioresAsync(DateTime limiteUtc, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

/// <summary>Un mensaje que el servidor mandó a un grupo de SignalR.</summary>
internal sealed record EnvioHub(string Grupo, string Metodo, object?[] Argumentos);

/// <summary>
/// Contexto del hub falso: anota qué se mandó a cada grupo en vez de abrir conexiones reales.
/// </summary>
internal sealed class HubContextFalso : IHubContext<SolarUY.Web.Hubs.MonitoreoHub>
{
    private readonly ConcurrentQueue<EnvioHub> _envios = new();

    public IReadOnlyCollection<EnvioHub> Envios => _envios.ToArray();

    public IHubClients Clients => new ClientesFalsos(_envios);

    public IGroupManager Groups => throw new NotSupportedException();

    /// <summary>Espera (con tope) a que haya un envío que cumpla la condición.</summary>
    public async Task EsperarAsync(Func<EnvioHub, bool> condicion, TimeSpan tope)
    {
        var limite = DateTime.UtcNow + tope;
        while (!_envios.Any(condicion))
        {
            if (DateTime.UtcNow > limite)
            {
                throw new TimeoutException("El mensaje esperado nunca se envió.");
            }

            await Task.Delay(20);
        }
    }

    private sealed class ClientesFalsos(ConcurrentQueue<EnvioHub> envios) : IHubClients
    {
        public IClientProxy All => throw new NotSupportedException();

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
            => throw new NotSupportedException();

        public IClientProxy Client(string connectionId) => throw new NotSupportedException();

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();

        public IClientProxy Group(string groupName) => new ProxyDeGrupo(groupName, envios);

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
            => throw new NotSupportedException();

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();

        public IClientProxy User(string userId) => throw new NotSupportedException();

        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private sealed class ProxyDeGrupo(string grupo, ConcurrentQueue<EnvioHub> envios) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            envios.Enqueue(new EnvioHub(grupo, method, args));
            return Task.CompletedTask;
        }
    }
}

/// <summary>Conexión falsa para probar el hub sin servidor: solo importa el id de conexión.</summary>
internal sealed class ContextoDeLlamadaFalso(string connectionId) : HubCallerContext
{
    public override string ConnectionId => connectionId;

    public override string? UserIdentifier => null;

    public override ClaimsPrincipal? User => null;

    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

    public override IFeatureCollection Features { get; } = new FeatureCollection();

    public override CancellationToken ConnectionAborted => CancellationToken.None;

    public override void Abort()
    {
    }
}

/// <summary>Administrador de grupos falso: anota a qué grupo se agregó cada conexión.</summary>
internal sealed class GruposFalsos : IGroupManager
{
    public List<(string ConnectionId, string Grupo)> Agregados { get; } = [];

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Agregados.Add((connectionId, groupName));
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
