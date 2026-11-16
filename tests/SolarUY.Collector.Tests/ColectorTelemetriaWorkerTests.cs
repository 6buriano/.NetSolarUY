using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SolarUY.Application.Sensors;
using SolarUY.Collector;
using SolarUY.Collector.Configuration;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Collector.Tests;

/// <summary>
/// Pruebas del worker que recolecta la telemetría de los sensores. Los repositorios y el cliente del
/// sensor son falsos, y el worker real corre una o dos rondas: se espera con un
/// <see cref="TaskCompletionSource"/> a que pase lo que se quiere observar, en vez de dormir.
/// </summary>
public class ColectorTelemetriaWorkerTests
{
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(10);
    private static readonly DateTime Momento = new(2026, 11, 15, 15, 0, 0, DateTimeKind.Utc);

    private sealed class Entorno
    {
        public required NodoRepositorioFalso Nodos { get; init; }
        public required TelemetriaRepositorioFalso Telemetria { get; init; }
        public required SensorFalso Sensor { get; init; }
        public required ColectorTelemetriaWorker Worker { get; init; }
    }

    private static Entorno Crear(IReadOnlyList<Nodo> nodos, ColectorOptions? opciones = null,
        Func<string, EstadoSensorDto?>? respuestaSensor = null, Exception? errorEnPrimeraRonda = null)
    {
        var repositorioNodos = new NodoRepositorioFalso(nodos, errorEnPrimeraRonda);
        var telemetria = new TelemetriaRepositorioFalso();
        var sensor = new SensorFalso(respuestaSensor ?? (_ => null));

        var servicios = new ServiceCollection()
            .AddSingleton<INodoRepository>(repositorioNodos)
            .AddSingleton<ITelemetriaRepository>(telemetria)
            .AddSingleton<ISensorClient>(sensor)
            .BuildServiceProvider();

        var worker = new ColectorTelemetriaWorker(
            servicios.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(opciones ?? new ColectorOptions()),
            NullLogger<ColectorTelemetriaWorker>.Instance);

        return new Entorno { Nodos = repositorioNodos, Telemetria = telemetria, Sensor = sensor, Worker = worker };
    }

    private static Nodo NuevoNodo(string nombre = "Nodo A", string hardware = "ARM-001", params string[] codigos)
    {
        var inversor = new Inversor("Huawei SUN2000", $"SN-{nombre}", 98d, true);
        inversor.RegistrarSensor(new SensorDispositivo(hardware, "1.0", true, "http://sensor-a:5100"));
        var nodo = new Nodo(nombre, "Sector 1", Guid.NewGuid(), inversor);

        foreach (var codigo in codigos)
        {
            nodo.Paneles.Add(new PanelSolar(codigo, "JAM72S30", "JA Solar", 550d, EstadoSalud.OPERATIVO,
                new DateTime(2026, 1, 1), nodo.Id));
        }

        return nodo;
    }

    private static LecturaSensorDto Lectura(string codigo, bool activo = true, double potenciaW = 400d)
        => new(codigo, activo, 8d, 40d, potenciaW, 35d);

    private static EstadoSensorDto Estado(string hardware, bool volcado, params LecturaSensorDto[] paneles)
        => new(hardware, Momento, volcado, 98d, paneles);

    private static async Task Detener(Entorno entorno)
    {
        await entorno.Worker.StopAsync(CancellationToken.None);
        entorno.Worker.Dispose();
    }

    // ---------------------------------------------------------------- Apagado y sin trabajo

    [Fact]
    public async Task SiElColectorEstaApagado_NoConsultaNadaYElWorkerTermina()
    {
        var entorno = Crear([NuevoNodo(codigos: "P01")], new ColectorOptions { Habilitado = false });

        await entorno.Worker.StartAsync(CancellationToken.None);
        await entorno.Worker.ExecuteTask!.WaitAsync(EsperaMaxima);

        Assert.Equal(0, entorno.Nodos.Consultas);
        Assert.Empty(entorno.Sensor.Consultados);
        await Detener(entorno);
    }

    [Fact]
    public async Task SinNodosConSensorActivo_NoLeeNingunSensor()
    {
        var entorno = Crear([]);

        await entorno.Worker.StartAsync(CancellationToken.None);
        await entorno.Nodos.PrimeraConsulta.WaitAsync(EsperaMaxima);
        await Detener(entorno);

        Assert.Empty(entorno.Sensor.Consultados);
        Assert.Empty(entorno.Telemetria.Guardadas);
    }

    // ---------------------------------------------------------------- Una ronda

    [Fact]
    public async Task UnaRonda_GuardaLasLecturasYActualizaElNodo()
    {
        var nodo = NuevoNodo(codigos: ["P01", "P02"]);
        var entorno = Crear([nodo], respuestaSensor: _ => Estado("ARM-001", false,
            Lectura("P01"), Lectura("P02", activo: false, potenciaW: 0d)));

        await entorno.Worker.StartAsync(CancellationToken.None);
        await entorno.Telemetria.PrimerGuardado.WaitAsync(EsperaMaxima);
        await Detener(entorno);

        Assert.Equal(2, entorno.Telemetria.Guardadas.Count);
        Assert.All(entorno.Telemetria.Guardadas, lectura => Assert.Equal(Momento, lectura.FechaHora));
        Assert.Same(nodo, Assert.Single(entorno.Nodos.Actualizados));
        Assert.Equal("http://sensor-a:5100", entorno.Sensor.UrlsConsultadas[0]);
        Assert.Equal("ARM-001", entorno.Sensor.Consultados[0]);
    }

    [Fact]
    public async Task UnaRonda_TomaDelSensorElVolcadoALaRedYLaActividadDeCadaPanel()
    {
        var nodo = NuevoNodo(codigos: ["P01", "P02"]);
        var entorno = Crear([nodo], respuestaSensor: _ => Estado("ARM-001", false,
            Lectura("P01"), Lectura("P02", activo: false, potenciaW: 0d)));

        await entorno.Worker.StartAsync(CancellationToken.None);
        await entorno.Telemetria.PrimerGuardado.WaitAsync(EsperaMaxima);
        await Detener(entorno);

        Assert.False(nodo.Inversor.VolcadoRedActivo);
        Assert.True(nodo.BuscarPanelPorCodigo("P01")!.Activo);
        var inactivo = nodo.BuscarPanelPorCodigo("P02")!;
        Assert.False(inactivo.Activo);
        Assert.Equal(EstadoSalud.DESCONECTADO, inactivo.Estado);
    }

    [Fact]
    public async Task UnaRonda_DiagnosticaLasLecturasAntesDeGuardarlas()
    {
        var nodo = NuevoNodo(codigos: ["P01", "P02", "P03"]);
        var entorno = Crear([nodo], respuestaSensor: _ => Estado("ARM-001", true,
            Lectura("P01", potenciaW: 400d), Lectura("P02", potenciaW: 400d), Lectura("P03", potenciaW: 100d)));

        await entorno.Worker.StartAsync(CancellationToken.None);
        await entorno.Telemetria.PrimerGuardado.WaitAsync(EsperaMaxima);
        await Detener(entorno);

        var caida = entorno.Telemetria.Guardadas.Single(lectura =>
            lectura.PanelSolarId == nodo.BuscarPanelPorCodigo("P03")!.Id);
        Assert.Equal(EstadoSalud.FALLA_DETECTADA, caida.EstadoDetectado);
    }

    [Fact]
    public async Task UnaRonda_IgnoraLosPanelesQueElSensorInformaYElNodoNoTiene()
    {
        var nodo = NuevoNodo(codigos: "P01");
        var entorno = Crear([nodo], respuestaSensor: _ => Estado("ARM-001", true,
            Lectura("P01"), Lectura("P99")));

        await entorno.Worker.StartAsync(CancellationToken.None);
        await entorno.Telemetria.PrimerGuardado.WaitAsync(EsperaMaxima);
        await Detener(entorno);

        var lectura = Assert.Single(entorno.Telemetria.Guardadas);
        Assert.Equal(nodo.BuscarPanelPorCodigo("P01")!.Id, lectura.PanelSolarId);
    }

    [Fact]
    public async Task UnaRonda_SiNingunPanelInformadoExisteEnElNodo_NoGuardaNada()
    {
        var entorno = Crear([NuevoNodo(codigos: "P01")], respuestaSensor: _ => Estado("ARM-001", true, Lectura("P99")));

        await entorno.Worker.StartAsync(CancellationToken.None);
        await entorno.Sensor.PrimeraConsulta.WaitAsync(EsperaMaxima);
        await Detener(entorno);

        Assert.Empty(entorno.Telemetria.Guardadas);
        Assert.Empty(entorno.Nodos.Actualizados);
    }

    [Fact]
    public async Task UnaRonda_SiUnSensorNoResponde_SeSaltaYSiguenLosDemas()
    {
        var caido = NuevoNodo("Caido", "ARM-001", "P01");
        var sano = NuevoNodo("Sano", "ARM-002", "P01");
        var entorno = Crear([caido, sano], respuestaSensor: hardware => hardware == "ARM-002"
            ? Estado("ARM-002", true, Lectura("P01"))
            : null);

        await entorno.Worker.StartAsync(CancellationToken.None);
        await entorno.Telemetria.PrimerGuardado.WaitAsync(EsperaMaxima);
        await Detener(entorno);

        Assert.Equal(["ARM-001", "ARM-002"], entorno.Sensor.Consultados.Take(2).ToArray());
        Assert.Same(sano, Assert.Single(entorno.Nodos.Actualizados));
        Assert.Single(entorno.Telemetria.Guardadas);
    }

    // ---------------------------------------------------------------- Resistencia

    [Fact]
    public async Task SiFallaUnaRonda_ElWorkerSigueYHaceLaSiguiente()
    {
        var nodo = NuevoNodo(codigos: "P01");
        var entorno = Crear([nodo], new ColectorOptions { IntervaloSegundos = 0 },
            _ => Estado("ARM-001", true, Lectura("P01")),
            errorEnPrimeraRonda: new InvalidOperationException("MySQL no responde"));

        await entorno.Worker.StartAsync(CancellationToken.None);
        await entorno.Telemetria.PrimerGuardado.WaitAsync(EsperaMaxima);
        await Detener(entorno);

        Assert.True(entorno.Nodos.Consultas >= 2);
        Assert.Single(entorno.Telemetria.Guardadas);
    }

    [Fact]
    public async Task AlDetenerElWorkerEnLaEspera_TerminaSinError()
    {
        var entorno = Crear([], new ColectorOptions { IntervaloSegundos = 3600 });

        await entorno.Worker.StartAsync(CancellationToken.None);
        await entorno.Nodos.PrimeraConsulta.WaitAsync(EsperaMaxima);
        await entorno.Worker.StopAsync(CancellationToken.None);

        Assert.True(entorno.Worker.ExecuteTask!.IsCompletedSuccessfully);
        entorno.Worker.Dispose();
    }

    // ---------------------------------------------------------------- Fakes

    private sealed class SensorFalso(Func<string, EstadoSensorDto?> respuesta) : ISensorClient
    {
        private readonly TaskCompletionSource _primera = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<string> Consultados { get; } = [];
        public List<string> UrlsConsultadas { get; } = [];
        public Task PrimeraConsulta => _primera.Task;

        public Task<EstadoSensorDto?> ObtenerEstadoAsync(string urlBase, string identificadorHardwareARM,
            CancellationToken cancellationToken = default)
        {
            UrlsConsultadas.Add(urlBase);
            Consultados.Add(identificadorHardwareARM);
            var estado = respuesta(identificadorHardwareARM);
            _primera.TrySetResult();
            return Task.FromResult(estado);
        }

        public Task<ResultadoOrdenSensor> ActivarPanelAsync(string urlBase, string identificadorHardwareARM,
            string codigoPanel, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ResultadoOrdenSensor> DesactivarPanelAsync(string urlBase, string identificadorHardwareARM,
            string codigoPanel, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ResultadoOrdenSensor> ActivarVolcadoAsync(string urlBase, string identificadorHardwareARM,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ResultadoOrdenSensor> DesactivarVolcadoAsync(string urlBase, string identificadorHardwareARM,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class NodoRepositorioFalso(IReadOnlyList<Nodo> nodos, Exception? errorEnPrimeraConsulta)
        : INodoRepository
    {
        private readonly TaskCompletionSource _primera = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _consultas;

        public int Consultas => Volatile.Read(ref _consultas);
        public Task PrimeraConsulta => _primera.Task;
        public List<Nodo> Actualizados { get; } = [];

        public Task<IReadOnlyList<Nodo>> ObtenerConSensoresActivosAsync(CancellationToken cancellationToken = default)
        {
            var numero = Interlocked.Increment(ref _consultas);
            _primera.TrySetResult();

            if (numero == 1 && errorEnPrimeraConsulta is not null)
            {
                throw errorEnPrimeraConsulta;
            }

            return Task.FromResult(nodos);
        }

        public Task ActualizarAsync(Nodo nodo, SensorDispositivo? sensorNuevo = null,
            CancellationToken cancellationToken = default)
        {
            Actualizados.Add(nodo);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Nodo>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Nodo>> ObtenerPorParqueAsync(Guid parqueSolarId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Nodo?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> ExisteNumeroSerieInversorAsync(string numeroSerie, Guid? nodoExcluidoId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> ExisteIdentificadorHardwareAsync(string identificadorHardwareARM,
            Guid? nodoExcluidoId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AgregarAsync(Nodo nodo, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task EliminarAsync(Nodo nodo, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class TelemetriaRepositorioFalso : ITelemetriaRepository
    {
        private readonly TaskCompletionSource _primero = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<LecturaTelemetria> Guardadas { get; } = [];
        public Task PrimerGuardado => _primero.Task;

        public Task AgregarVariasAsync(IReadOnlyList<LecturaTelemetria> lecturas,
            CancellationToken cancellationToken = default)
        {
            Guardadas.AddRange(lecturas);
            _primero.TrySetResult();
            return Task.CompletedTask;
        }

        public Task AgregarAsync(LecturaTelemetria lectura, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<LecturaTelemetria>> ObtenerUltimasPorPanelAsync(Guid panelSolarId, int cantidad,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<AgregadoPotencia>> ObtenerAgregadoPorPanelAsync(Guid panelSolarId,
            GranularidadConsulta granularidad, DateTime? desde = null, DateTime? hasta = null,
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
}
