namespace SolarUY.Application.Sensors;

/// <summary>
/// Abstracción del sensor ARM + inversor de un nodo (spec del simulador, sección 8).
/// Es la frontera que permite reemplazar el simulador por hardware real sin tocar el resto
/// del sistema: la implementación concreta vive en Infrastructure.
/// </summary>
public interface ISensorClient
{
    /// <summary>
    /// Consulta el estado del nodo, es decir las lecturas de todos sus paneles.
    /// Devuelve <c>null</c> si el sensor no responde o contesta que no existe: que un sensor
    /// falle no puede frenar a los demás.
    /// </summary>
    Task<EstadoSensorDto?> ObtenerEstadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default);

    Task<ResultadoOrdenSensor> ActivarPanelAsync(string urlBase, string identificadorHardwareARM,
        string codigoPanel, CancellationToken cancellationToken = default);

    Task<ResultadoOrdenSensor> DesactivarPanelAsync(string urlBase, string identificadorHardwareARM,
        string codigoPanel, CancellationToken cancellationToken = default);

    Task<ResultadoOrdenSensor> ActivarVolcadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default);

    Task<ResultadoOrdenSensor> DesactivarVolcadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default);
}
