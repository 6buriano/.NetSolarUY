using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SolarUY.Application.Sensors;
using SolarUY.Collector;
using SolarUY.Collector.Configuration;
using SolarUY.Domain.Enums;
using SolarUY.Infrastructure.Persistence;

namespace SolarUY.IntegrationTests;

/// <summary>
/// Ingesta: el <see cref="ColectorTelemetriaWorker"/> real consulta al sensor (de mentira), el dominio
/// diagnostica y todo queda guardado en MySQL con el esquema de las migraciones.
/// </summary>
public class IngestaTelemetriaTests : IAsyncLifetime
{
    private static readonly DateTime FechaSensor = new(2026, 11, 11, 14, 0, 0, DateTimeKind.Utc);

    private readonly EntornoMySql _entorno = new();

    public Task InitializeAsync() => _entorno.InitializeAsync();

    public Task DisposeAsync() => _entorno.DisposeAsync();

    [FactMySql]
    public async Task UnaRonda_GuardaUnaLecturaPorPanelConLosValoresDelSensor()
    {
        var nodoId = await NodoConPanelesAsync("ARM-001", "P01", "P02");
        _entorno.Sensores.Responder("ARM-001", Estado("ARM-001",
            Lectura("P01", potencia: 250, temperatura: 40),
            Lectura("P02", potencia: 240, temperatura: 41)));

        await EjecutarUnaRondaAsync(lecturasEsperadas: 2);

        using var contexto = NuevoContexto();
        var lecturas = await contexto.LecturasTelemetria.AsNoTracking().ToListAsync();
        Assert.Equal(2, lecturas.Count);
        Assert.All(lecturas, lectura => Assert.Equal(FechaSensor, lectura.FechaHora));

        var paneles = await contexto.Paneles.AsNoTracking().Where(p => p.NodoId == nodoId).ToListAsync();
        var p01 = paneles.Single(p => p.CodigoPanel == "P01");
        var lecturaP01 = lecturas.Single(l => l.PanelSolarId == p01.Id);
        Assert.Equal(250, lecturaP01.PotenciaGeneradaWatts);
        Assert.Equal(40, lecturaP01.TemperaturaCelsius);
    }

    [FactMySql]
    public async Task UnaRonda_ConSobretemperatura_DiagnosticaFallaYActualizaElPanel()
    {
        var nodoId = await NodoConPanelesAsync("ARM-001", "P01", "P02");
        _entorno.Sensores.Responder("ARM-001", Estado("ARM-001",
            Lectura("P01", potencia: 250, temperatura: 40),
            Lectura("P02", potencia: 250, temperatura: 90)));

        await EjecutarUnaRondaAsync(lecturasEsperadas: 2);

        using var contexto = NuevoContexto();
        var paneles = await contexto.Paneles.AsNoTracking().Where(p => p.NodoId == nodoId).ToListAsync();
        Assert.Equal(EstadoSalud.OPERATIVO, paneles.Single(p => p.CodigoPanel == "P01").Estado);
        var panelEnFalla = paneles.Single(p => p.CodigoPanel == "P02");
        Assert.Equal(EstadoSalud.FALLA_DETECTADA, panelEnFalla.Estado);

        var idPanelEnFalla = panelEnFalla.Id;
        var lecturaEnFalla = await contexto.LecturasTelemetria.AsNoTracking()
            .SingleAsync(l => l.PanelSolarId == idPanelEnFalla);
        Assert.Equal(EstadoSalud.FALLA_DETECTADA, lecturaEnFalla.EstadoDetectado);
    }

    [FactMySql]
    public async Task UnaRonda_ConPanelInactivo_LoDejaDesconectadoYSincronizaLaActividad()
    {
        var nodoId = await NodoConPanelesAsync("ARM-001", "P01", "P02");
        _entorno.Sensores.Responder("ARM-001", Estado("ARM-001",
            Lectura("P01", potencia: 250, temperatura: 40),
            Lectura("P02", potencia: 0, temperatura: 25, activo: false)));

        await EjecutarUnaRondaAsync(lecturasEsperadas: 2);

        using var contexto = NuevoContexto();
        var inactivo = await contexto.Paneles.AsNoTracking()
            .SingleAsync(p => p.NodoId == nodoId && p.CodigoPanel == "P02");
        Assert.False(inactivo.Activo);
        Assert.Equal(EstadoSalud.DESCONECTADO, inactivo.Estado);
    }

    [FactMySql]
    public async Task UnaRonda_SincronizaElVolcadoALaRedConLoQueInformaElSensor()
    {
        var nodoId = await NodoConPanelesAsync("ARM-001", "P01");
        _entorno.Sensores.Responder("ARM-001",
            Estado("ARM-001", volcadoRed: false, Lectura("P01", potencia: 250, temperatura: 40)));

        await EjecutarUnaRondaAsync(lecturasEsperadas: 1);

        using var contexto = NuevoContexto();
        var nodo = await contexto.Nodos.AsNoTracking().Include(n => n.Inversor).SingleAsync(n => n.Id == nodoId);
        Assert.False(nodo.Inversor.VolcadoRedActivo);
    }

    [FactMySql]
    public async Task UnaRonda_IgnoraElPanelQueElSensorInformaPeroElNodoNoTiene()
    {
        await NodoConPanelesAsync("ARM-001", "P01");
        _entorno.Sensores.Responder("ARM-001", Estado("ARM-001",
            Lectura("P01", potencia: 250, temperatura: 40),
            Lectura("P99", potencia: 250, temperatura: 40)));

        await EjecutarUnaRondaAsync(lecturasEsperadas: 1);

        using var contexto = NuevoContexto();
        Assert.Equal(1, await contexto.LecturasTelemetria.CountAsync());
    }

    [FactMySql]
    public async Task UnaRonda_SiUnSensorNoResponde_LosOtrosNodosSiguenGuardando()
    {
        var parqueId = await _entorno.Servicios.CrearParqueAsync();
        var caido = await _entorno.Servicios.CrearNodoAsync(parqueId, "Nodo A", "ARM-001");
        await _entorno.Servicios.CrearPanelAsync(caido, "P01");
        var sano = await _entorno.Servicios.CrearNodoAsync(parqueId, "Nodo B", "ARM-002");
        await _entorno.Servicios.CrearPanelAsync(sano, "P01");

        _entorno.Sensores.Responder("ARM-001", null);
        _entorno.Sensores.Responder("ARM-002", Estado("ARM-002", Lectura("P01", potencia: 200, temperatura: 40)));

        await EjecutarUnaRondaAsync(lecturasEsperadas: 1);

        using var contexto = NuevoContexto();
        var lectura = await contexto.LecturasTelemetria.AsNoTracking().SingleAsync();
        var panelDelNodoSano = await contexto.Paneles.AsNoTracking().SingleAsync(p => p.NodoId == sano);
        Assert.Equal(panelDelNodoSano.Id, lectura.PanelSolarId);
    }

    [FactMySql]
    public async Task UnaRonda_NoConsultaLosNodosSinSensor()
    {
        var parqueId = await _entorno.Servicios.CrearParqueAsync();
        var sinSensor = await _entorno.Servicios.CrearNodoAsync(parqueId, "Nodo sin sensor", hardware: null);
        await _entorno.Servicios.CrearPanelAsync(sinSensor, "P01");
        var conSensor = await _entorno.Servicios.CrearNodoAsync(parqueId, "Nodo con sensor", "ARM-001");
        await _entorno.Servicios.CrearPanelAsync(conSensor, "P01");
        _entorno.Sensores.Responder("ARM-001", Estado("ARM-001", Lectura("P01", potencia: 200, temperatura: 40)));

        await EjecutarUnaRondaAsync(lecturasEsperadas: 1);

        Assert.Equal(1, _entorno.Sensores.Consultas);
    }

    // ---------------------------------------------------------------------------------------

    private async Task<Guid> NodoConPanelesAsync(string hardware, params string[] codigos)
    {
        var parqueId = await _entorno.Servicios.CrearParqueAsync();
        var nodoId = await _entorno.Servicios.CrearNodoAsync(parqueId, "Nodo 1", hardware);
        foreach (var codigo in codigos)
        {
            await _entorno.Servicios.CrearPanelAsync(nodoId, codigo);
        }

        return nodoId;
    }

    /// <summary>
    /// Arranca el worker real con un intervalo larguísimo (así hace exactamente una ronda), espera a que
    /// las lecturas esperadas estén en la base y lo detiene.
    /// </summary>
    private async Task EjecutarUnaRondaAsync(int lecturasEsperadas)
    {
        var worker = new ColectorTelemetriaWorker(
            _entorno.Servicios.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ColectorOptions { Habilitado = true, IntervaloSegundos = 3600 }),
            NullLogger<ColectorTelemetriaWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            var limite = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < limite)
            {
                using var contexto = NuevoContexto();
                if (await contexto.LecturasTelemetria.CountAsync() >= lecturasEsperadas
                    && _entorno.Sensores.Consultas > 0)
                {
                    // Un margen corto para que termine de guardar el estado de los paneles, que se
                    // persiste justo antes que las lecturas.
                    await Task.Delay(200);
                    return;
                }

                await Task.Delay(100);
            }

            Assert.Fail($"El colector no guardó las {lecturasEsperadas} lecturas esperadas en 30 segundos.");
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private SolarUYDbContext NuevoContexto()
        => _entorno.Servicios.CreateScope().ServiceProvider.GetRequiredService<SolarUYDbContext>();

    private static EstadoSensorDto Estado(string hardware, params LecturaSensorDto[] paneles)
        => Estado(hardware, volcadoRed: true, paneles);

    private static EstadoSensorDto Estado(string hardware, bool volcadoRed, params LecturaSensorDto[] paneles)
        => new(hardware, FechaSensor, volcadoRed, 97.5, paneles);

    private static LecturaSensorDto Lectura(string codigo, double potencia, double temperatura, bool activo = true)
        => new(codigo, activo, CorrienteContinua: 8, Voltaje: 30, potencia, temperatura);
}
