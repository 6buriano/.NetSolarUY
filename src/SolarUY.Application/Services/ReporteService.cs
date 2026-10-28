using SolarUY.Application.DTOs;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Services;

/// <summary>
/// Consultas históricas de generación: agrega la potencia de un panel por día, mes o año.
/// </summary>
public class ReporteService
{
    /// <summary>
    /// Cada cuántos segundos guarda una ronda el colector (<c>Colector:IntervaloSegundos</c>
    /// en su appsettings; 5 s por defecto en todo el proyecto). Sirve para estimar la energía
    /// generada a partir de la potencia instantánea: no es una medición exacta, es la mejor
    /// aproximación posible sin cambiar lo que guarda el colector (potencia, no energía).
    /// </summary>
    public const int IntervaloLecturaSegundos = 5;

    private static readonly string[] Meses =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
    ];

    private readonly ITelemetriaRepository _telemetriaRepository;

    public ReporteService(ITelemetriaRepository telemetriaRepository)
    {
        _telemetriaRepository = telemetriaRepository;
    }

    public async Task<IReadOnlyList<ResumenGeneracionDto>> ObtenerGeneracionPorPanelAsync(Guid panelSolarId,
        GranularidadConsulta granularidad, DateTime? desde = null, DateTime? hasta = null,
        CancellationToken cancellationToken = default)
    {
        var agregados = await _telemetriaRepository.ObtenerAgregadoPorPanelAsync(panelSolarId, granularidad, desde,
            hasta, cancellationToken);

        return agregados
            .Select(agregado => new ResumenGeneracionDto(
                agregado.PeriodoInicio,
                Etiquetar(agregado.PeriodoInicio, granularidad),
                agregado.CantidadLecturas,
                Math.Round(agregado.PotenciaPromedioW, 2),
                Math.Round(agregado.PotenciaMaximaW, 2),
                // Wh = Σ(potencia en W de cada lectura) × intervalo en horas; se pasa a kWh.
                Math.Round(agregado.PotenciaSumaW * IntervaloLecturaSegundos / 3600d / 1000d, 4)))
            .ToList();
    }

    private static string Etiquetar(DateTime periodoInicio, GranularidadConsulta granularidad) => granularidad switch
    {
        GranularidadConsulta.DIA => periodoInicio.ToString("dd/MM/yyyy"),
        GranularidadConsulta.MES => $"{Meses[periodoInicio.Month - 1]} {periodoInicio.Year}",
        GranularidadConsulta.ANIO => periodoInicio.Year.ToString(),
        _ => periodoInicio.ToString("dd/MM/yyyy")
    };
}
