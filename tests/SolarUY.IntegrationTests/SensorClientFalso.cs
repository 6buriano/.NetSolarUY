using SolarUY.Application.Sensors;

namespace SolarUY.IntegrationTests;

/// <summary>
/// Sensor de mentira: contesta lo que el test le configura para cada identificador de hardware.
/// Si no hay nada configurado (o se configuró <c>null</c>) se comporta como un sensor caído.
/// </summary>
public sealed class SensorClientFalso : ISensorClient
{
    private readonly Dictionary<string, EstadoSensorDto?> _respuestas = new(StringComparer.OrdinalIgnoreCase);
    private int _consultas;

    public int Consultas => Volatile.Read(ref _consultas);

    public void Responder(string identificadorHardware, EstadoSensorDto? estado)
        => _respuestas[identificadorHardware] = estado;

    public Task<EstadoSensorDto?> ObtenerEstadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _consultas);
        _respuestas.TryGetValue(identificadorHardwareARM, out var estado);
        return Task.FromResult(estado);
    }

    public Task<ResultadoOrdenSensor> ActivarPanelAsync(string urlBase, string identificadorHardwareARM,
        string codigoPanel, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<ResultadoOrdenSensor> DesactivarPanelAsync(string urlBase, string identificadorHardwareARM,
        string codigoPanel, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<ResultadoOrdenSensor> ActivarVolcadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<ResultadoOrdenSensor> DesactivarVolcadoAsync(string urlBase, string identificadorHardwareARM,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
