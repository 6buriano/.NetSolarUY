using System.Globalization;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using SolarUY.Application.Consultas;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;

namespace SolarUY.Infrastructure.Consultas;

/// <summary>
/// Cliente del agente: obtiene las herramientas del servidor MCP, se las pasa a Gemini junto con la
/// pregunta y devuelve la respuesta. El ciclo de "Gemini pide una herramienta, se ejecuta y se le
/// devuelve el resultado" lo resuelve la librería; acá solo se conecta el MCP con el chat.
/// </summary>
public sealed class GeminiMcpAgentService : IMCPAgentService
{
    private const string InstruccionSistema =
        "Sos el asistente de SolarUY, un sistema de monitoreo de parques solares. " +
        "Respondé siempre en español, en texto plano: sin Markdown, sin asteriscos, sin negritas y sin " +
        "viñetas con símbolos. Si necesitás enumerar, usá líneas separadas o números seguidos de un punto. " +
        "Respondé en pocas frases y solo con los datos que devuelvan las herramientas: no inventes " +
        "números, fechas ni identificadores. " +
        "Si necesitás el identificador de un parque, usá primero la herramienta listar_parques. " +
        "Si la pregunta es ajena al sistema (parques, paneles, nodos, generación y fallas) o pide dar " +
        "una orden a un panel o a un sensor, respondé que solo podés consultar el estado y la generación " +
        "de los parques solares, y no ejecutes ninguna acción. " +
        "La energía histórica y la de hoy son estimaciones calculadas a partir de la potencia " +
        "instantánea: aclaralo cada vez que informes energía. " +
        "\"Hoy\" es el día calendario de Montevideo. Para fechas relativas (\"este mes\", \"ayer\", " +
        "\"la semana pasada\") tomá como referencia la fecha de hoy que se te indica. " +
        "Si no hay datos para responder, decilo con claridad en vez de suponer.";

    private readonly IChatClient _cliente;
    private readonly string _urlServidorMcp;
    private readonly TimeProvider _reloj;

    public GeminiMcpAgentService(IChatClient cliente, string urlServidorMcp, TimeProvider reloj)
    {
        _cliente = cliente;
        _urlServidorMcp = urlServidorMcp;
        _reloj = reloj;
    }

    public async Task<ConsultaLenguajeNaturalResponse> ConsultarAsync(string pregunta,
        CancellationToken cancellationToken = default)
    {
        var opcionesTransporte = new HttpClientTransportOptions { Endpoint = new Uri(_urlServidorMcp) };
        await using var transporte = new HttpClientTransport(opcionesTransporte);
        await using var servidorMcp = await McpClient.CreateAsync(transporte,
            cancellationToken: cancellationToken);

        var herramientas = await servidorMcp.ListToolsAsync(cancellationToken: cancellationToken);

        var mensajes = new[]
        {
            new ChatMessage(ChatRole.System, ConstruirInstruccion()),
            new ChatMessage(ChatRole.User, pregunta)
        };

        var opciones = new ChatOptions
        {
            Tools = herramientas.Cast<AITool>().ToList(),
            // Baja: las respuestas son datos, no texto creativo, y queremos que repita lo que midió.
            Temperature = 0.2f
        };

        var respuesta = await _cliente.GetResponseAsync(mensajes, opciones, cancellationToken);

        return new ConsultaLenguajeNaturalResponse(true, respuesta.Text ?? string.Empty);
    }

    /// <summary>
    /// Instrucción de sistema de esta consulta: la constante más la fecha de hoy en Montevideo, que es
    /// la referencia para las fechas relativas ("este mes", "ayer").
    /// </summary>
    private string ConstruirInstruccion()
    {
        var fechaDeHoy = _reloj.GetUtcNow()
            .Add(ReporteService.DesfaseMontevideo)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return $"{InstruccionSistema} Fecha de hoy en Montevideo: {fechaDeHoy}.";
    }
}
