using SolarUY.Application.Diagnostics;
using SolarUY.Application.Sensors;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Services;

/// <summary>
/// Verifica que los nodos, sensores y paneles cargados en el sistema coincidan con el hardware que
/// emula el simulador. Es una herramienta de diagnóstico: no participa del flujo normal de la
/// aplicación, y por eso es lo único que depende de <see cref="ISimuladorAdminClient"/>.
/// </summary>
public class VerificacionVinculacionService
{
    private readonly INodoRepository _nodoRepository;
    private readonly ISimuladorAdminClient _simuladorAdminClient;

    public VerificacionVinculacionService(INodoRepository nodoRepository, ISimuladorAdminClient simuladorAdminClient)
    {
        _nodoRepository = nodoRepository;
        _simuladorAdminClient = simuladorAdminClient;
    }

    public async Task<InformeVinculacion> VerificarAsync(CancellationToken cancellationToken = default)
    {
        var nodos = await _nodoRepository.ObtenerTodosAsync(cancellationToken);

        // El simulador atiende a todos sus sensores en la misma dirección, así que se pregunta una
        // vez por cada dirección distinta que tengan cargada los nodos.
        var direcciones = nodos
            .Select(nodo => nodo.Inversor?.Sensor?.UrlBase)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (direcciones.Count == 0)
        {
            return InformeVinculacion.SinSimulador(
                "Ningún nodo tiene cargada la dirección de su sensor, así que no hay a dónde preguntarle el inventario.",
                nodos.Count);
        }

        var inventario = new List<InventarioSensorDto>();
        var sinRespuesta = new List<string>();

        foreach (var direccion in direcciones)
        {
            var deEstaDireccion = await _simuladorAdminClient.ObtenerInventarioAsync(direccion, cancellationToken);

            if (deEstaDireccion is null)
            {
                sinRespuesta.Add(direccion);
            }
            else
            {
                inventario.AddRange(deEstaDireccion);
            }
        }

        if (inventario.Count == 0)
        {
            return InformeVinculacion.SinSimulador(
                $"No se pudo leer el inventario en {string.Join(", ", sinRespuesta)}. " +
                "Revisá que el simulador esté corriendo: docker compose up -d sensor-simulator.",
                nodos.Count);
        }

        return ComparadorVinculacion.Comparar(nodos, inventario);
    }
}
