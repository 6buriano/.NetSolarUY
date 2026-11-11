using SolarUY.Application.DTOs;

namespace SolarUY.Application.Consultas;

/// <summary>
/// Responde preguntas en lenguaje natural sobre el estado y la generación del sistema, usando las
/// herramientas MCP de solo lectura.
/// </summary>
/// <remarks>
/// La firma no menciona ni a Gemini ni al protocolo MCP: son el detalle de implementación que pone
/// <c>Infrastructure</c>. La aplicación solo sabe que hay un agente al que se le hace una pregunta.
/// </remarks>
public interface IMCPAgentService
{
    /// <summary>
    /// Devuelve la respuesta en español a la pregunta. No lanza por fallas del proveedor de IA (sin
    /// red, sin clave, cuota agotada o tiempo excedido): en esos casos devuelve
    /// <see cref="ConsultaLenguajeNaturalResponse"/> con <c>Exito</c> en <c>false</c> y el mensaje
    /// para mostrar al usuario.
    /// </summary>
    Task<ConsultaLenguajeNaturalResponse> ConsultarAsync(string pregunta,
        CancellationToken cancellationToken = default);
}
