namespace SolarUY.Domain.Enums;

/// <summary>
/// Estado de salud de un panel solar o de una lectura de telemetría.
/// </summary>
public enum EstadoSalud
{
    OPERATIVO,
    RENDIMIENTO_BAJO,
    FALLA_DETECTADA,
    DESCONECTADO
}
