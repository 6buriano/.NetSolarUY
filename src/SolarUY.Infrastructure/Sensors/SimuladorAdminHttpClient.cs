using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SolarUY.Application.Sensors;

namespace SolarUY.Infrastructure.Sensors;

/// <summary>
/// Cliente HTTP del inventario del simulador. Es lo que permite comparar los datos del sistema
/// contra el hardware que realmente está emulado.
/// </summary>
public class SimuladorAdminHttpClient : ISimuladorAdminClient
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<SimuladorAdminHttpClient> _logger;

    public SimuladorAdminHttpClient(HttpClient httpClient, ILogger<SimuladorAdminHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InventarioSensorDto>?> ObtenerInventarioAsync(string urlBase,
        CancellationToken cancellationToken = default)
    {
        var url = $"{urlBase.TrimEnd('/')}/simulador/inventario";

        try
        {
            using var respuesta = await _httpClient.GetAsync(url, cancellationToken);

            if (respuesta.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("En {UrlBase} no hay un simulador: /simulador/inventario no existe.", urlBase);
                return null;
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("El simulador en {UrlBase} respondió {Codigo} al pedir el inventario.",
                    urlBase, (int)respuesta.StatusCode);
                return null;
            }

            var inventario = await respuesta.Content.ReadFromJsonAsync<List<InventarioSensorDto>>(
                OpcionesJson, cancellationToken);

            return inventario;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("El simulador en {UrlBase} no respondió a tiempo.", urlBase);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo consultar el inventario del simulador en {UrlBase}.", urlBase);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "El simulador en {UrlBase} devolvió un inventario que no se entiende.", urlBase);
            return null;
        }
    }
}
