namespace SolarUY.Application.Sensors;

/// <summary>
/// API de administración del <b>simulador</b>, no del sensor.
/// </summary>
/// <remarks>
/// El contrato real del sensor (el de <see cref="ISensorClient"/>) no tiene forma de preguntar
/// "¿qué hardware existe?": un sensor de verdad simplemente es el que es. Este inventario es una
/// herramienta del mock para demos y para verificar que los datos del sistema coincidan con el
/// hardware. Por eso vive en una abstracción aparte y solo lo usan herramientas de diagnóstico:
/// el producto no depende de ella.
/// </remarks>
public interface ISimuladorAdminClient
{
    /// <summary>
    /// Devuelve el inventario de sensores del simulador, o <c>null</c> si no responde o no es un
    /// simulador (por ejemplo, si contra esa dirección hay hardware real).
    /// </summary>
    Task<IReadOnlyList<InventarioSensorDto>?> ObtenerInventarioAsync(string urlBase,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Le pide al simulador que empiece a emular un sensor nuevo (POST /simulador/sensores).
    /// Devuelve el sensor creado, o <c>null</c> si no se pudo.
    /// </summary>
    /// <remarks>
    /// Esto no tiene equivalente en el mundo real —un inversor se instala, no se crea por HTTP—,
    /// así que solo lo usa la provisión de nodos de prueba.
    /// </remarks>
    Task<InventarioSensorDto?> CrearSensorAsync(string urlBase, string identificadorHardwareARM,
        int cantidadPaneles, double potenciaNominalW, CancellationToken cancellationToken = default);
}
