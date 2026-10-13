namespace SolarUY.Application.Sensors;

/// <summary>
/// Lectura de un panel tal como la reporta el sensor (spec del simulador, sección 3).
/// El sensor sólo mide: no informa el estado de salud, eso lo diagnostica el dominio.
/// </summary>
public record LecturaSensorDto(
    string CodigoPanel,
    bool Activo,
    double CorrienteContinua,
    double Voltaje,
    double PotenciaGeneradaWatts,
    double TemperaturaCelsius);
