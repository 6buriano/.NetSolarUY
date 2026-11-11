using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SolarUY.Application.Services;

namespace SolarUY.Infrastructure.Mcp.Tools;

/// <summary>
/// Herramienta MCP que devuelve la generación estimada de hoy sumando todos los parques.
/// </summary>
[McpServerToolType]
public sealed class ObtenerGeneracionHoyTool
{
    private readonly ReporteService _reporteService;

    public ObtenerGeneracionHoyTool(ReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    /// <summary>Devuelve la generación de hoy, o un texto aclarando que todavía no hay lecturas.</summary>
    [McpServerTool(Name = "obtener_generacion_hoy")]
    [Description(
        "Devuelve la generación estimada de hoy sumando todos los parques, en JSON con los campos " +
        "etiqueta, cantidadLecturas y energiaEstimadaKWh. \"Hoy\" es el día calendario de Montevideo " +
        "(UTC−3), desde las 00:00 hasta el momento de la consulta. energiaEstimadaKWh es una " +
        "estimación calculada a partir de la potencia instantánea que registra el colector, no una " +
        "medición de energía.")]
    public async Task<string> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        var generacion = await _reporteService.ObtenerGeneracionHoyAsync(cancellationToken);

        if (generacion.CantidadLecturas == 0)
        {
            return "Todavía no hay lecturas registradas hoy, así que no hay generación para informar.";
        }

        return JsonSerializer.Serialize(generacion, OpcionesJsonMcp.Opciones);
    }
}
