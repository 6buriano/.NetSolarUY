using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SolarUY.Application.Services;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Infrastructure.Mcp.Tools;

/// <summary>
/// Herramienta MCP que devuelve el resumen de un parque: cuántos nodos y paneles tiene y cómo se
/// reparten los paneles por estado.
/// </summary>
[McpServerToolType]
public sealed class ObtenerResumenParqueTool
{
    private readonly ParqueService _parqueService;

    public ObtenerResumenParqueTool(ParqueService parqueService)
    {
        _parqueService = parqueService;
    }

    /// <summary>
    /// Devuelve el resumen del parque indicado, o un texto explicando por qué no se pudo.
    /// </summary>
    [McpServerTool(Name = "obtener_resumen_parque")]
    [Description(
        "Devuelve el resumen de un parque en JSON, con estos campos: nombre, ubicacion, cantidadNodos, " +
        "cantidadPaneles, panelesOperativos, panelesRendimientoBajo, panelesEnFalla y " +
        "panelesDesconectados. Los cuatro conteos por estado suman cantidadPaneles. Usala para saber " +
        "cómo está funcionando un parque.")]
    public async Task<string> ObtenerAsync(
        [Description("Id del parque, tal como lo devuelve la herramienta listar_parques.")] string parqueId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(parqueId, out var id))
        {
            return "El identificador del parque no es válido. Tiene que ser el id que devuelve la " +
                "herramienta listar_parques.";
        }

        try
        {
            var resumen = await _parqueService.ObtenerResumenAsync(id, cancellationToken);
            return JsonSerializer.Serialize(resumen, OpcionesJsonMcp.Opciones);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            // Una regla de negocio es un resultado esperado ("el parque no existe"): su mensaje está
            // escrito para el usuario y la herramienta lo devuelve como texto. Cualquier otra
            // excepción sigue subiendo a propósito: no es un resultado, es una falla que hay que ver.
            return excepcion.Message;
        }
    }
}
