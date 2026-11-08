using SolarUY.Application.DTOs;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;
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
    private readonly IParqueRepository _parqueRepository;
    private readonly TimeProvider _timeProvider;

    public ReporteService(ITelemetriaRepository telemetriaRepository, IParqueRepository parqueRepository,
        TimeProvider timeProvider)
    {
        _telemetriaRepository = telemetriaRepository;
        _parqueRepository = parqueRepository;
        _timeProvider = timeProvider;
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
                CalcularEnergiaEstimadaKWh(agregado.PotenciaSumaW)))
            .ToList();
    }

    /// <summary>
    /// Generación estimada de un parque, agrupada por día, mes o año.
    /// </summary>
    /// <exception cref="ReglaDeNegocioException">Si el parque no existe.</exception>
    public async Task<IReadOnlyList<GeneracionPeriodoDto>> ObtenerGeneracionAsync(Guid parqueSolarId,
        GranularidadConsulta granularidad, DateOnly? desde, DateOnly? hasta,
        CancellationToken cancellationToken = default)
    {
        var parque = await _parqueRepository.ObtenerPorIdAsync(parqueSolarId, cancellationToken);
        if (parque is null)
        {
            throw new ReglaDeNegocioException("El parque indicado no existe.");
        }

        if (desde is not null && hasta is not null && desde > hasta)
        {
            throw new ReglaDeNegocioException("La fecha desde no puede ser posterior a la fecha hasta.");
        }

        // Las fechas llegan sin hora: desde las 00:00 del primer día hasta el último instante del
        // último día, para que "hasta" incluya ese día completo (lo mismo que hace HistoricoController).
        var agregados = await _telemetriaRepository.ObtenerAgregadoGeneralAsync(parqueSolarId, granularidad,
            desde?.ToDateTime(TimeOnly.MinValue),
            hasta?.ToDateTime(TimeOnly.MaxValue),
            cancellationToken);

        return agregados
            .Select(agregado => new GeneracionPeriodoDto(
                Etiquetar(agregado.PeriodoInicio, granularidad),
                agregado.CantidadLecturas,
                CalcularEnergiaEstimadaKWh(agregado.PotenciaSumaW)))
            .ToList();
    }

    /// <summary>
    /// Estimación de energía de un período, en kWh, redondeada a 4 decimales.
    /// </summary>
    /// <remarks>
    /// Wh = Σ(potencia en W de cada lectura) × intervalo en horas; después se pasa a kWh. No es una
    /// medición exacta: el colector guarda potencia instantánea cada
    /// <see cref="IntervaloLecturaSegundos"/> segundos, no energía.
    /// </remarks>
    private static double CalcularEnergiaEstimadaKWh(double potenciaSumaW)
        => Math.Round(potenciaSumaW * IntervaloLecturaSegundos / 3600d / 1000d, 4);

    private static string Etiquetar(DateTime periodoInicio, GranularidadConsulta granularidad) => granularidad switch
    {
        GranularidadConsulta.DIA => periodoInicio.ToString("dd/MM/yyyy"),
        GranularidadConsulta.MES => $"{Meses[periodoInicio.Month - 1]} {periodoInicio.Year}",
        GranularidadConsulta.ANIO => periodoInicio.Year.ToString(),
        _ => periodoInicio.ToString("dd/MM/yyyy")
    };
}
