namespace SolarUY.Application.DTOs;

/// <summary>
/// Datos del sensor ARM que se registra junto con el inversor de un nodo.
/// Es lo que le dice al colector a qué dirección preguntarle por la telemetría.
/// </summary>
public record SensorDispositivoRequest(
    string IdentificadorHardwareARM,
    string VersionFirmware,
    string UrlBase,
    bool Activo);
