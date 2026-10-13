using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SolarUY.Application.Sensors;

namespace SolarUY.Infrastructure.Sensors;

/// <summary>
/// Cliente HTTP del sensor ARM (spec del simulador, secciones 3 y 8). Es el único lugar del
/// sistema que conoce el contrato HTTP del hardware: si mañana se reemplaza el simulador por
/// sensores reales, se cambia esta clase y nada más.
/// </summary>
public class SensorHttpClient : ISensorClient
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<SensorHttpClient> _logger;

    public SensorHttpClient(HttpClient httpClient, ILogger<SensorHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EstadoSensorDto?> ObtenerEstadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
    {
        var url = ArmarUrl(urlBase, identificadorHardwareARM, "estado");

        try
        {
            using var respuesta = await _httpClient.GetAsync(url, cancellationToken);

            if (respuesta.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("El sensor {Hardware} no existe en {UrlBase}.", identificadorHardwareARM, urlBase);
                return null;
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("El sensor {Hardware} respondió {Codigo}.", identificadorHardwareARM,
                    (int)respuesta.StatusCode);
                return null;
            }

            var estado = await respuesta.Content.ReadFromJsonAsync<EstadoSensorRespuesta>(OpcionesJson, cancellationToken);

            if (estado is null)
            {
                _logger.LogWarning("El sensor {Hardware} devolvió una respuesta vacía.", identificadorHardwareARM);
                return null;
            }

            return Mapear(estado);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // El token propio del HttpClient venció: el sensor no contesta. Si la cancelación viene
            // de afuera (apagado del servicio) la excepción sube, que es lo correcto.
            _logger.LogWarning("El sensor {Hardware} no respondió a tiempo.", identificadorHardwareARM);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo consultar al sensor {Hardware}.", identificadorHardwareARM);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "El sensor {Hardware} devolvió un JSON que no se entiende.", identificadorHardwareARM);
            return null;
        }
    }

    public Task<ResultadoOrdenSensor> ActivarPanelAsync(string urlBase, string identificadorHardwareARM,
        string codigoPanel, CancellationToken cancellationToken = default)
        => EnviarOrdenAsync(ArmarUrl(urlBase, identificadorHardwareARM, "paneles", codigoPanel, "activar"),
            identificadorHardwareARM, cancellationToken);

    public Task<ResultadoOrdenSensor> DesactivarPanelAsync(string urlBase, string identificadorHardwareARM,
        string codigoPanel, CancellationToken cancellationToken = default)
        => EnviarOrdenAsync(ArmarUrl(urlBase, identificadorHardwareARM, "paneles", codigoPanel, "desactivar"),
            identificadorHardwareARM, cancellationToken);

    public Task<ResultadoOrdenSensor> ActivarVolcadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
        => EnviarOrdenAsync(ArmarUrl(urlBase, identificadorHardwareARM, "volcado", "activar"),
            identificadorHardwareARM, cancellationToken);

    public Task<ResultadoOrdenSensor> DesactivarVolcadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
        => EnviarOrdenAsync(ArmarUrl(urlBase, identificadorHardwareARM, "volcado", "desactivar"),
            identificadorHardwareARM, cancellationToken);

    private async Task<ResultadoOrdenSensor> EnviarOrdenAsync(string url, string identificadorHardwareARM,
        CancellationToken cancellationToken)
    {
        try
        {
            using var respuesta = await _httpClient.PostAsync(url, content: null, cancellationToken);

            switch (respuesta.StatusCode)
            {
                case HttpStatusCode.OK:
                    return ResultadoOrdenSensor.Ok;
                case HttpStatusCode.NotFound:
                    _logger.LogWarning("El sensor {Hardware} o el panel pedido no existen.", identificadorHardwareARM);
                    return ResultadoOrdenSensor.NoEncontrado;
                case HttpStatusCode.Conflict:
                    return ResultadoOrdenSensor.YaAplicado;
                default:
                    _logger.LogWarning("El sensor {Hardware} rechazó la orden con {Codigo}.", identificadorHardwareARM,
                        (int)respuesta.StatusCode);
                    return ResultadoOrdenSensor.SinRespuesta;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("El sensor {Hardware} no respondió a tiempo a la orden.", identificadorHardwareARM);
            return ResultadoOrdenSensor.SinRespuesta;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo mandar la orden al sensor {Hardware}.", identificadorHardwareARM);
            return ResultadoOrdenSensor.SinRespuesta;
        }
    }

    /// <summary>
    /// Arma la URL del sensor. Todos los segmentos se escapan porque el identificador de hardware
    /// y el código de panel viajan en la ruta.
    /// </summary>
    private static string ArmarUrl(string urlBase, string identificadorHardwareARM, params string[] segmentos)
    {
        var partes = new List<string>
        {
            urlBase.TrimEnd('/'),
            "sensores",
            Uri.EscapeDataString(identificadorHardwareARM)
        };

        partes.AddRange(segmentos.Select(Uri.EscapeDataString));

        return string.Join('/', partes);
    }

    private static EstadoSensorDto Mapear(EstadoSensorRespuesta estado)
        => new(
            estado.IdentificadorHardwareARM,
            DateTime.SpecifyKind(estado.FechaHoraUtc, DateTimeKind.Utc),
            estado.VolcadoRedActivo,
            // El sensor reporta la eficiencia como fracción (0,97) y el dominio la usa en porcentaje.
            // Se redondea porque 0,97 * 100 da 96,99999999999999 en punto flotante.
            Math.Round(estado.EficienciaConversion * 100d, 4),
            estado.Paneles
                .Select(panel => new LecturaSensorDto(
                    panel.CodigoPanel,
                    panel.Activo,
                    panel.CorrienteContinua,
                    panel.Voltaje,
                    panel.PotenciaGeneradaWatts,
                    panel.TemperaturaCelsius))
                .ToList());
}

/// <summary>Forma exacta del JSON del sensor (sección 3 de la spec). Es privado del cliente.</summary>
internal sealed record EstadoSensorRespuesta(
    string IdentificadorHardwareARM,
    DateTime FechaHoraUtc,
    bool VolcadoRedActivo,
    double EficienciaConversion,
    List<LecturaPanelRespuesta> Paneles);

internal sealed record LecturaPanelRespuesta(
    string CodigoPanel,
    bool Activo,
    double CorrienteContinua,
    double Voltaje,
    double PotenciaGeneradaWatts,
    double TemperaturaCelsius);
