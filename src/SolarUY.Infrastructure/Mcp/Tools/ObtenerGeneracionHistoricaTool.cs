using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Server;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Infrastructure.Mcp.Tools;

/// <summary>
/// Herramienta MCP que devuelve la generación histórica de un parque, agrupada por día, mes o año.
/// </summary>
[McpServerToolType]
public sealed class ObtenerGeneracionHistoricaTool
{
    /// <summary>
    /// Máximo de períodos que devuelve la herramienta. Un año agrupado por día son 366 filas; 400 deja
    /// margen para eso y corta los rangos de varios años, que generarían un JSON enorme que el modelo
    /// no puede leer y una respuesta inútil. Ante ese caso se le pide acotar el rango o agrupar más
    /// grueso, en vez de truncar los datos sin avisar.
    /// </summary>
    private const int MaximoPeriodos = 400;

    private const string FormatoFecha = "yyyy-MM-dd";

    private readonly ReporteService _reporteService;

    public ObtenerGeneracionHistoricaTool(ReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    /// <summary>
    /// Devuelve los períodos del rango pedido, o un texto explicando por qué no se pudo.
    /// </summary>
    [McpServerTool(Name = "obtener_generacion_historica")]
    [Description(
        "Devuelve la generación histórica de un parque agrupada por día, mes o año, en JSON con los " +
        "campos agrupacion, totalLecturas, totalEnergiaEstimadaKWh y periodos. La agrupación es por " +
        "día UTC, no por día de Montevideo. Las fechas van en formato yyyy-MM-dd y son opcionales: si " +
        "se omiten, el rango queda abierto de ese lado. La fecha hasta incluye todo ese día. " +
        "Cada elemento de periodos tiene etiqueta, cantidadLecturas y energiaEstimadaKWh. " +
        "energiaEstimadaKWh es una estimación calculada a partir de la potencia instantánea que " +
        "registra el colector, no una medición de energía.")]
    public async Task<string> ObtenerAsync(
        [Description("Id del parque, tal como lo devuelve la herramienta listar_parques.")]
        string parqueId,
        [Description("Cómo agrupar los períodos: dia, mes o anio.")]
        string agrupacion,
        [Description("Fecha desde, formato yyyy-MM-dd. Opcional: si se omite, el rango no tiene límite inferior.")]
        string? desde = null,
        [Description("Fecha hasta, formato yyyy-MM-dd (incluye ese día completo). Opcional: si se omite, el rango no tiene límite superior.")]
        string? hasta = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(parqueId, out var idParque))
        {
            return "El identificador del parque no es válido. Tiene que ser el id que devuelve la " +
                "herramienta listar_parques.";
        }

        if (!TryParsearAgrupacion(agrupacion, out var granularidad))
        {
            return "La agrupación no es válida. Los valores posibles son dia, mes o anio.";
        }

        if (!TryParsearFecha(desde, out var fechaDesde))
        {
            return $"La fecha desde no es válida. El formato es {FormatoFecha} (por ejemplo 2026-11-08).";
        }

        if (!TryParsearFecha(hasta, out var fechaHasta))
        {
            return $"La fecha hasta no es válida. El formato es {FormatoFecha} (por ejemplo 2026-11-08).";
        }

        IReadOnlyList<GeneracionPeriodoDto> periodos;

        try
        {
            periodos = await _reporteService.ObtenerGeneracionAsync(idParque, granularidad, fechaDesde,
                fechaHasta, cancellationToken);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            // Parque inexistente o rango invertido: son resultados esperados y su mensaje está escrito
            // para el usuario. Cualquier otra excepción sigue subiendo a propósito.
            return excepcion.Message;
        }

        if (periodos.Count == 0)
        {
            return "No hay lecturas para ese parque en ese rango de fechas.";
        }

        if (periodos.Count > MaximoPeriodos)
        {
            return $"El rango devuelve {periodos.Count} períodos y el máximo es {MaximoPeriodos}. " +
                "Acotá el rango de fechas o agrupá por mes o año.";
        }

        var salida = new
        {
            Agrupacion = NombreDeAgrupacion(granularidad),
            TotalLecturas = periodos.Sum(periodo => periodo.CantidadLecturas),
            TotalEnergiaEstimadaKWh = Math.Round(periodos.Sum(periodo => periodo.EnergiaEstimadaKWh), 4),
            Periodos = periodos
        };

        return JsonSerializer.Serialize(salida, OpcionesJsonMcp.Opciones);
    }

    /// <summary>Acepta dia/mes/anio con o sin tilde y sin distinguir mayúsculas.</summary>
    private static bool TryParsearAgrupacion(string? agrupacion, out GranularidadConsulta granularidad)
    {
        switch (agrupacion?.Trim().ToLowerInvariant())
        {
            case "dia":
            case "día":
                granularidad = GranularidadConsulta.DIA;
                return true;

            case "mes":
                granularidad = GranularidadConsulta.MES;
                return true;

            case "anio":
            case "año":
                granularidad = GranularidadConsulta.ANIO;
                return true;

            default:
                granularidad = GranularidadConsulta.DIA;
                return false;
        }
    }

    /// <summary>Parsea una fecha opcional en formato yyyy-MM-dd; vacía significa "sin límite".</summary>
    private static bool TryParsearFecha(string? valor, out DateOnly? fecha)
    {
        fecha = null;

        if (string.IsNullOrWhiteSpace(valor))
        {
            return true;
        }

        if (DateOnly.TryParseExact(valor.Trim(), FormatoFecha, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fechaParseada))
        {
            fecha = fechaParseada;
            return true;
        }

        return false;
    }

    private static string NombreDeAgrupacion(GranularidadConsulta granularidad) => granularidad switch
    {
        GranularidadConsulta.MES => "mes",
        GranularidadConsulta.ANIO => "anio",
        _ => "dia"
    };
}
