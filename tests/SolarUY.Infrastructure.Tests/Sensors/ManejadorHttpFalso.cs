using System.Net;
using System.Text;

namespace SolarUY.Infrastructure.Tests.Sensors;

/// <summary>
/// Manejador HTTP falso: no sale a la red. Devuelve lo que se le configure y anota cada pedido,
/// así se prueban los clientes HTTP sin levantar el simulador.
/// </summary>
internal sealed class ManejadorHttpFalso(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respuesta)
    : HttpMessageHandler
{
    public List<string> Pedidos { get; } = [];
    public List<string?> Cuerpos { get; } = [];

    public static ManejadorHttpFalso Responde(HttpStatusCode codigo, string? json = null)
        => new((_, _) => Task.FromResult(Armar(codigo, json)));

    public static ManejadorHttpFalso Falla(Exception excepcion)
        => new((_, _) => throw excepcion);

    public static HttpResponseMessage Armar(HttpStatusCode codigo, string? json)
        => new(codigo)
        {
            Content = json is null ? new StringContent(string.Empty) : new StringContent(json, Encoding.UTF8, "application/json")
        };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Pedidos.Add($"{request.Method} {request.RequestUri!.AbsoluteUri}");
        Cuerpos.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
        return await respuesta(request, cancellationToken);
    }
}
