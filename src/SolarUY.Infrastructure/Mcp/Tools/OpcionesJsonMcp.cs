using System.Text.Encodings.Web;
using System.Text.Json;

namespace SolarUY.Infrastructure.Mcp.Tools;

/// <summary>
/// Opciones de serialización que comparten todas las herramientas MCP, para que respondan con el
/// mismo formato.
/// </summary>
internal static class OpcionesJsonMcp
{
    /// <summary>
    /// camelCase como el resto de las APIs del sistema, y
    /// <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/> para no escapar caracteres que el
    /// JSON no necesita escapar (las tildes y la ñ quedan legibles). Es seguro acá porque la salida
    /// la lee un modelo de lenguaje, no se inserta en HTML ni en JavaScript: no hay superficie de
    /// inyección. Va compartida para que todas las herramientas usen exactamente el mismo formato.
    /// </summary>
    internal static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
