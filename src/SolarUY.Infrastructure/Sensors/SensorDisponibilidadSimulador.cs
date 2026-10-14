using Microsoft.Extensions.Logging;
using SolarUY.Application.Sensors;

namespace SolarUY.Infrastructure.Sensors;

/// <summary>
/// Implementación para desarrollo: le pregunta al inventario del simulador si un identificador
/// existe, y le pide que cree sensores nuevos cuando hace falta.
/// </summary>
public class SensorDisponibilidadSimulador : ISensorDisponibilidadService
{
    /// <summary>Si el simulador no tiene ningún sensor del que copiar la forma, se usan estos valores.</summary>
    private const int PanelesPorDefecto = 5;
    private const double PotenciaPorDefecto = 300;

    private readonly ISimuladorAdminClient _simuladorAdminClient;
    private readonly string _urlSimulador;
    private readonly ILogger<SensorDisponibilidadSimulador> _logger;

    public SensorDisponibilidadSimulador(ISimuladorAdminClient simuladorAdminClient, string urlSimulador,
        ILogger<SensorDisponibilidadSimulador> logger)
    {
        _simuladorAdminClient = simuladorAdminClient;
        _urlSimulador = urlSimulador;
        _logger = logger;
    }

    public async Task<DisponibilidadSensor> VerificarAsync(string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
    {
        var inventario = await _simuladorAdminClient.ObtenerInventarioAsync(_urlSimulador, cancellationToken);

        if (inventario is null)
        {
            return new DisponibilidadSensor(
                VerificacionPosible: true,
                SePudoConsultar: false,
                Existe: false,
                IdentificadoresExistentes: []);
        }

        return new DisponibilidadSensor(
            VerificacionPosible: true,
            SePudoConsultar: true,
            Existe: inventario.Any(sensor => string.Equals(sensor.IdentificadorHardwareARM, identificadorHardwareARM,
                StringComparison.OrdinalIgnoreCase)),
            IdentificadoresExistentes: inventario.Select(sensor => sensor.IdentificadorHardwareARM).ToList());
    }

    public async Task<ResultadoAltaSensor> CrearAsync(string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
    {
        // Se copia la forma de los sensores que ya existen, así el nodo nuevo queda con la misma
        // topología que el resto del parque (misma cantidad de paneles y misma potencia nominal).
        var paneles = (await _simuladorAdminClient.ObtenerInventarioAsync(_urlSimulador, cancellationToken))
            ?.FirstOrDefault()?.Paneles;

        var cantidadPaneles = paneles is { Count: > 0 } ? paneles.Count : PanelesPorDefecto;
        var potenciaNominal = paneles?.FirstOrDefault()?.PotenciaNominalW ?? PotenciaPorDefecto;

        var creado = await _simuladorAdminClient.CrearSensorAsync(_urlSimulador, identificadorHardwareARM,
            cantidadPaneles, potenciaNominal, cancellationToken);

        if (creado is null)
        {
            return new ResultadoAltaSensor(
                Creado: false,
                Error: $"No se pudo crear el sensor '{identificadorHardwareARM}' en el simulador. " +
                       "Mirá el log de la aplicación para el motivo exacto.",
                CodigosDePanel: []);
        }

        _logger.LogInformation("El simulador empezó a emular el sensor {Hardware} con {Paneles} paneles.",
            creado.IdentificadorHardwareARM, creado.Paneles.Count);

        return new ResultadoAltaSensor(
            Creado: true,
            Error: null,
            CodigosDePanel: creado.Paneles.Select(panel => panel.CodigoPanel).ToList());
    }
}
