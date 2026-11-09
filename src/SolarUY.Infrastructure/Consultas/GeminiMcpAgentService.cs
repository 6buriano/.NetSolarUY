using System.Globalization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
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
    /// <summary>Texto único que ve el usuario cuando la consulta no se pudo completar.</summary>
    public const string MensajeDeFalla =
        "No se pudo obtener una respuesta en este momento. Probá de nuevo en unos minutos.";

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
    private readonly TimeSpan _timeout;
    private readonly ILogger<GeminiMcpAgentService> _logger;

    public GeminiMcpAgentService(IChatClient cliente, string urlServidorMcp, TimeProvider reloj,
        TimeSpan timeout, ILogger<GeminiMcpAgentService> logger)
    {
        _cliente = cliente;
        _urlServidorMcp = urlServidorMcp;
        _reloj = reloj;
        _timeout = timeout;
        _logger = logger;
    }

    public async Task<ConsultaLenguajeNaturalResponse> ConsultarAsync(string pregunta,
        CancellationToken cancellationToken = default)
    {
        using var tokenDeTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        tokenDeTimeout.CancelAfter(_timeout);

        try
        {
            var opcionesTransporte = new HttpClientTransportOptions
            {
                Endpoint = new Uri(_urlServidorMcp)
            };

            await using var transporte = new HttpClientTransport(opcionesTransporte);
            await using var servidorMcp = await McpClient.CreateAsync(transporte,
                cancellationToken: tokenDeTimeout.Token);

            return await ConversarAsync(servidorMcp, pregunta, tokenDeTimeout.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Lo canceló quien llamó (por ejemplo, cerró la página): no es una falla del proveedor y
            // no hay que disfrazarla de respuesta, así que sigue subiendo.
            throw;
        }
        catch (Exception excepcion)
        {
            // Excepción DELIBERADA a la regla 5 §4 ("no capturar Exception genérica"). Esa regla
            // protege a los controladores de tapar bugs; acá el contrato de IMCPAgentService es no
            // lanzar nunca por fallas del proveedor (sin red, clave inválida, cuota, timeout, URL
            // mal formada, JSON roto), y esos son tipos que no se pueden enumerar de antemano sin
            // que la lista quede incompleta y el usuario vea una página de error. Se registra el
            // detalle y se devuelve el mensaje único para el usuario.
            var fueTimeout = tokenDeTimeout.IsCancellationRequested;

            if (fueTimeout)
            {
                _logger.LogWarning(
                    "La consulta al agente superó el timeout de {Timeout}. Excepción: {TipoDeExcepcion}",
                    _timeout, DescribirTipo(excepcion));
            }
            else
            {
                _logger.LogError(
                    "Falló la consulta al agente. Excepción: {TipoDeExcepcion}",
                    DescribirTipo(excepcion));
            }

            return new ConsultaLenguajeNaturalResponse(false, MensajeDeFalla);
        }
    }

    private async Task<ConsultaLenguajeNaturalResponse> ConversarAsync(McpClient servidorMcp,
        string pregunta, CancellationToken cancellationToken)
    {
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

        if (string.IsNullOrWhiteSpace(respuesta.Text))
        {
            _logger.LogWarning("El modelo devolvió una respuesta vacía.");
            return new ConsultaLenguajeNaturalResponse(false, MensajeDeFalla);
        }

        return new ConsultaLenguajeNaturalResponse(true, respuesta.Text);
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

    /// <summary>
    /// Cadena de tipos de la excepción (externa e internas), sin mensajes. La clave de Gemini puede
    /// viajar en la URL del pedido, así que nunca se registran mensajes ni URLs: solo qué falló.
    /// </summary>
    private static string DescribirTipo(Exception excepcion)
    {
        var tipos = new List<string>();
        for (var actual = excepcion; actual is not null; actual = actual.InnerException)
        {
            tipos.Add(actual.GetType().FullName ?? actual.GetType().Name);
        }

        return string.Join(" -> ", tipos);
    }
}
