namespace SolarUY.Application.DTOs;

/// <summary>Datos del sensor ARM que reporta la telemetría de un inversor.</summary>
public record SensorDispositivoDto(
    Guid Id,
    string IdentificadorHardwareARM,
    string VersionFirmware,
    bool Activo,
    string UrlBase);
