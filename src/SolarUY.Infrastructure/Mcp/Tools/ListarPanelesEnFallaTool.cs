using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SolarUY.Application.Services;
using SolarUY.Domain.Enums;

namespace SolarUY.Infrastructure.Mcp.Tools;

/// <summary>
/// Herramienta MCP que lista los paneles que están en falla en este momento, con su nodo.
/// </summary>
[McpServerToolType]
public sealed class ListarPanelesEnFallaTool
{
    /// <summary>
    /// Máximo de paneles que se devuelven en el JSON. Un parque grande puede tener cientos de paneles
    /// en falla; 200 filas ya son un JSON de decenas de KB y el modelo no las aprovecha. Cuando se
    /// supera, se devuelve igual el total (para que pueda decir cuántos son) y los primeros 200,
    /// avisando que la lista está recortada: nunca se oculta el total.
    /// </summary>
    private const int MaximoPanelesEnFalla = 200;

    private readonly PanelService _panelService;

    public ListarPanelesEnFallaTool(PanelService panelService)
    {
        _panelService = panelService;
    }

    /// <summary>Devuelve los paneles en falla, o un texto si no hay ninguno.</summary>
    [McpServerTool(Name = "listar_paneles_en_falla")]
    [Description(
        "Devuelve los paneles que están en falla en este momento, en JSON con los campos " +
        "cantidadEnFalla y paneles (y aviso cuando la lista viene recortada). Cada panel trae id, " +
        "codigoPanel, modelo, fabricante, potenciaNominalW, estado, activo, fechaInstalacion, nodoId " +
        "y nodoNombre. \"En falla\" es solo el estado FALLA_DETECTADA: los paneles desconectados o " +
        "con rendimiento bajo no entran en esta lista.")]
    public async Task<string> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        var paneles = await _panelService.ObtenerTodosAsync(EstadoSalud.FALLA_DETECTADA, cancellationToken);

        if (paneles.Count == 0)
        {
            return "Buenas noticias: ningún panel está en falla en este momento.";
        }

        if (paneles.Count > MaximoPanelesEnFalla)
        {
            var recortada = new
            {
                CantidadEnFalla = paneles.Count,
                Aviso = $"Se muestran los primeros {MaximoPanelesEnFalla} de {paneles.Count} paneles " +
                    "en falla.",
                Paneles = paneles.Take(MaximoPanelesEnFalla).ToList()
            };

            return JsonSerializer.Serialize(recortada, OpcionesJsonMcp.Opciones);
        }

        var completa = new
        {
            CantidadEnFalla = paneles.Count,
            Paneles = paneles
        };

        return JsonSerializer.Serialize(completa, OpcionesJsonMcp.Opciones);
    }
}
