using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SolarUY.Application.Services;

namespace SolarUY.Infrastructure.Mcp.Tools;

/// <summary>
/// Herramienta MCP que lista los parques con su Id. Existe para que el agente obtenga el
/// <c>parqueId</c> que necesitan las demás herramientas: no puede adivinarlo.
/// </summary>
[McpServerToolType]
public sealed class ListarParquesTool
{
    private readonly ParqueService _parqueService;

    public ListarParquesTool(ParqueService parqueService)
    {
        _parqueService = parqueService;
    }

    /// <summary>Devuelve todos los parques en JSON, con el Id de cada uno.</summary>
    [McpServerTool(Name = "listar_parques")]
    [Description(
        "Devuelve todos los parques solares en JSON, cada uno con estos campos: id, nombre, ubicacion, " +
        "capacidadInstaladaKW, fechaInstalacion, cantidadNodos, cantidadPaneles y potenciaNominalPanelesKW. " +
        "capacidadInstaladaKW es la capacidad declarada del parque; potenciaNominalPanelesKW es la suma " +
        "de la potencia nominal de los paneles cargados, así que no tienen por qué coincidir. " +
        "El id es el parqueId que hay que pasar a las demás herramientas.")]
    public async Task<string> ListarAsync(CancellationToken cancellationToken = default)
    {
        var parques = await _parqueService.ObtenerTodosAsync(cancellationToken);
        return JsonSerializer.Serialize(parques, OpcionesJsonMcp.Opciones);
    }
}
