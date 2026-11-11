using Microsoft.Extensions.DependencyInjection;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;
using SolarUY.Domain.Repositories;

namespace SolarUY.IntegrationTests;

/// <summary>
/// Consultas: <see cref="ReporteService"/> y el repositorio de telemetría contra MySQL, con lecturas
/// de fechas y potencias conocidas para poder verificar cada número a mano.
/// </summary>
public class ConsultasHistoricasTests : IAsyncLifetime
{
    private readonly EntornoMySql _entorno = new();

    public Task InitializeAsync() => _entorno.InitializeAsync();

    public Task DisposeAsync() => _entorno.DisposeAsync();

    [FactMySql]
    public async Task PorPanel_AgrupaPorDia_ConConteoPromedioMaximoYEnergia()
    {
        var panel = await PanelAsync();
        await GuardarAsync(panel,
            (Utc(2026, 11, 9, 12), 100), (Utc(2026, 11, 9, 13), 300),
            (Utc(2026, 11, 10, 12), 200));

        var dias = await Reporte(r => r.ObtenerGeneracionPorPanelAsync(panel, GranularidadConsulta.DIA));

        Assert.Equal(2, dias.Count);
        // Del más nuevo al más viejo, que es como la pantalla muestra la tabla.
        Assert.Equal("10/11/2026", dias[0].Etiqueta);
        Assert.Equal(1, dias[0].CantidadLecturas);
        Assert.Equal(200, dias[0].PotenciaPromedioW);

        Assert.Equal("09/11/2026", dias[1].Etiqueta);
        Assert.Equal(2, dias[1].CantidadLecturas);
        Assert.Equal(200, dias[1].PotenciaPromedioW);
        Assert.Equal(300, dias[1].PotenciaMaximaW);
        // (100 + 300) W × 5 s / 3600 / 1000
        Assert.Equal(Math.Round(400 * 5 / 3600d / 1000d, 4), dias[1].EnergiaEstimadaKWh);
    }

    [FactMySql]
    public async Task PorPanel_AgrupaPorMesYPorAnio()
    {
        var panel = await PanelAsync();
        await GuardarAsync(panel,
            (Utc(2025, 12, 31, 12), 100),
            (Utc(2026, 1, 5, 12), 200),
            (Utc(2026, 1, 20, 12), 400),
            (Utc(2026, 2, 1, 12), 300));

        var meses = await Reporte(r => r.ObtenerGeneracionPorPanelAsync(panel, GranularidadConsulta.MES));
        Assert.Equal(["febrero 2026", "enero 2026", "diciembre 2025"], meses.Select(m => m.Etiqueta));
        Assert.Equal(2, meses.Single(m => m.Etiqueta == "enero 2026").CantidadLecturas);
        Assert.Equal(300, meses.Single(m => m.Etiqueta == "enero 2026").PotenciaPromedioW);

        var anios = await Reporte(r => r.ObtenerGeneracionPorPanelAsync(panel, GranularidadConsulta.ANIO));
        Assert.Equal(["2026", "2025"], anios.Select(a => a.Etiqueta));
        Assert.Equal(3, anios[0].CantidadLecturas);
        Assert.Equal(400, anios[0].PotenciaMaximaW);
    }

    [FactMySql]
    public async Task PorPanel_RespetaElRangoDeFechasInclusivo()
    {
        var panel = await PanelAsync();
        await GuardarAsync(panel,
            (Utc(2026, 11, 8, 23), 100),
            (Utc(2026, 11, 9, 0), 100),
            (Utc(2026, 11, 10, 23), 100),
            (Utc(2026, 11, 11, 0), 100));

        var dias = await Reporte(r => r.ObtenerGeneracionPorPanelAsync(panel, GranularidadConsulta.DIA,
            desde: new DateTime(2026, 11, 9), hasta: new DateTime(2026, 11, 10, 23, 59, 59)));

        Assert.Equal(["10/11/2026", "09/11/2026"], dias.Select(d => d.Etiqueta));
    }

    [FactMySql]
    public async Task PorPanel_NoMezclaLasLecturasDeOtrosPaneles()
    {
        var parque = await _entorno.Servicios.CrearParqueAsync();
        var nodo = await _entorno.Servicios.CrearNodoAsync(parque, "Nodo 1", "ARM-001");
        var p1 = await _entorno.Servicios.CrearPanelAsync(nodo, "P01");
        var p2 = await _entorno.Servicios.CrearPanelAsync(nodo, "P02");
        await GuardarAsync(p1, (Utc(2026, 11, 10, 12), 100));
        await GuardarAsync(p2, (Utc(2026, 11, 10, 12), 999), (Utc(2026, 11, 10, 13), 999));

        var dias = await Reporte(r => r.ObtenerGeneracionPorPanelAsync(p1, GranularidadConsulta.DIA));

        var dia = Assert.Single(dias);
        Assert.Equal(1, dia.CantidadLecturas);
        Assert.Equal(100, dia.PotenciaMaximaW);
    }

    [FactMySql]
    public async Task PorParque_SoloSumaLosPanelesDeEseParque()
    {
        var parqueA = await _entorno.Servicios.CrearParqueAsync("Parque A");
        var parqueB = await _entorno.Servicios.CrearParqueAsync("Parque B");
        var nodoA = await _entorno.Servicios.CrearNodoAsync(parqueA, "Nodo A", "ARM-001");
        var nodoB = await _entorno.Servicios.CrearNodoAsync(parqueB, "Nodo B", "ARM-002");
        var panelA = await _entorno.Servicios.CrearPanelAsync(nodoA, "P01");
        var panelB = await _entorno.Servicios.CrearPanelAsync(nodoB, "P01");
        await GuardarAsync(panelA, (Utc(2026, 11, 10, 12), 100), (Utc(2026, 11, 10, 13), 100));
        await GuardarAsync(panelB, (Utc(2026, 11, 10, 12), 500));

        var deA = await Reporte(r => r.ObtenerGeneracionAsync(parqueA, GranularidadConsulta.DIA, null, null));
        var deB = await Reporte(r => r.ObtenerGeneracionAsync(parqueB, GranularidadConsulta.DIA, null, null));

        Assert.Equal(2, Assert.Single(deA).CantidadLecturas);
        Assert.Equal(Math.Round(200 * 5 / 3600d / 1000d, 4), deA[0].EnergiaEstimadaKWh);
        Assert.Equal(1, Assert.Single(deB).CantidadLecturas);
    }

    [FactMySql]
    public async Task PorParque_ConParqueInexistente_LanzaReglaDeNegocio()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            Reporte(r => r.ObtenerGeneracionAsync(Guid.NewGuid(), GranularidadConsulta.DIA, null, null)));
    }

    [FactMySql]
    public async Task GeneracionHoy_UsaElDiaDeMontevideoYNoElDiaUtc()
    {
        var panel = await PanelAsync();
        // Son las 01:00 UTC del 11/11 = 22:00 del 10/11 en Montevideo (UTC-3).
        // El "hoy" de Montevideo empieza el 10/11 a las 03:00 UTC.
        _entorno.Reloj.Fijar(new DateTimeOffset(2026, 11, 11, 1, 0, 0, TimeSpan.Zero));
        await GuardarAsync(panel,
            (Utc(2026, 11, 10, 2), 1000),   // 23:00 del 9/11 en Montevideo: es de ayer, no cuenta.
            (Utc(2026, 11, 10, 12), 100),   // Hoy.
            (Utc(2026, 11, 11, 0, 30), 200)); // Hoy también, aunque ya sea otro día UTC.

        var hoy = await Reporte(r => r.ObtenerGeneracionHoyAsync());

        Assert.Equal("hoy", hoy.Etiqueta);
        Assert.Equal(2, hoy.CantidadLecturas);
        Assert.Equal(Math.Round(300 * 5 / 3600d / 1000d, 4), hoy.EnergiaEstimadaKWh);
    }

    [FactMySql]
    public async Task Retencion_BorraSoloLasLecturasMasViejasQueElLimite()
    {
        var panel = await PanelAsync();
        // Reloj en 11/11/2026 15:00 UTC; con 90 días el límite es el 13/08/2026 15:00 UTC.
        await GuardarAsync(panel,
            (Utc(2026, 8, 1, 0), 100),
            (Utc(2026, 8, 13, 14), 100),
            (Utc(2026, 8, 13, 16), 100),
            (Utc(2026, 11, 10, 12), 100));

        int borradas;
        using (var scope = _entorno.Servicios.CreateScope())
        {
            borradas = await scope.ServiceProvider.GetRequiredService<RetencionTelemetriaService>().PurgarAsync(90);
        }

        Assert.Equal(2, borradas);
        var restantes = await Reporte(r => r.ObtenerGeneracionPorPanelAsync(panel, GranularidadConsulta.DIA));
        Assert.Equal(["10/11/2026", "13/08/2026"], restantes.Select(d => d.Etiqueta));
    }

    // ---------------------------------------------------------------------------------------

    private static DateTime Utc(int anio, int mes, int dia, int hora, int minuto = 0)
        => new(anio, mes, dia, hora, minuto, 0, DateTimeKind.Utc);

    private async Task<Guid> PanelAsync()
    {
        var parque = await _entorno.Servicios.CrearParqueAsync();
        var nodo = await _entorno.Servicios.CrearNodoAsync(parque, "Nodo 1", "ARM-001");
        return await _entorno.Servicios.CrearPanelAsync(nodo, "P01");
    }

    private async Task GuardarAsync(Guid panelId, params (DateTime FechaHora, double Potencia)[] lecturas)
    {
        using var scope = _entorno.Servicios.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<ITelemetriaRepository>();
        await repositorio.AgregarVariasAsync(lecturas
            .Select(l => new LecturaTelemetria(panelId, l.FechaHora, 8, 30, l.Potencia, 40))
            .ToList());
    }

    private async Task<T> Reporte<T>(Func<ReporteService, Task<T>> consulta)
    {
        using var scope = _entorno.Servicios.CreateScope();
        return await consulta(scope.ServiceProvider.GetRequiredService<ReporteService>());
    }
}
