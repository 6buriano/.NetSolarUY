using SolarUY.Application.Sensors;

namespace SolarUY.Infrastructure.Sensors;

/// <summary>
/// Implementación para cuando no hay un simulador al que preguntarle: producción, o un entorno de
/// desarrollo sin el mock levantado.
/// </summary>
/// <remarks>
/// No verifica nada y no deja crear sensores, así que el ABM se comporta como antes de que existiera
/// esta función: crea el nodo con el identificador que le den.
/// </remarks>
public class SensorDisponibilidadNula : ISensorDisponibilidadService
{
    public Task<DisponibilidadSensor> VerificarAsync(string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new DisponibilidadSensor(
            VerificacionPosible: false,
            SePudoConsultar: false,
            Existe: false,
            IdentificadoresExistentes: []));

    public Task<ResultadoAltaSensor> CrearAsync(string identificadorHardwareARM,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ResultadoAltaSensor(
            Creado: false,
            Error: "Este entorno no puede crear sensores: no hay un simulador con API de administración.",
            CodigosDePanel: []));
}
