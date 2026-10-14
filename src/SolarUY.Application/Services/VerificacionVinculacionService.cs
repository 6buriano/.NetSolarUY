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

    /// <param name="urlSimulador">
    /// Dirección del simulador a consultar. Se recibe por parámetro (viene de la configuración) y no
    /// se deduce de los nodos, para que la verificación funcione incluso sin ningún nodo cargado.
    /// </param>
    public async Task<InformeVinculacion> VerificarAsync(string urlSimulador,
        CancellationToken cancellationToken = default)
    {
        var nodos = await _nodoRepository.ObtenerTodosAsync(cancellationToken);

        var inventario = await _simuladorAdminClient.ObtenerInventarioAsync(urlSimulador, cancellationToken);

        if (inventario is null)
        {
            return InformeVinculacion.SinSimulador(
                $"No se pudo leer el inventario en {urlSimulador}. " +
                "Revisá que el simulador esté corriendo: docker compose up -d sensor-simulator.",
                nodos.Count);
        }

        return ComparadorVinculacion.Comparar(nodos, inventario);
    }
}
