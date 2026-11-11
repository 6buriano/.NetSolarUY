using SolarUY.Domain.Enums;

namespace SolarUY.Application.DTOs;

/// <summary>
/// Estado actual de un panel en el monitoreo. Potencia, temperatura y hora son nulas si el panel
/// no tiene una lectura reciente (por ejemplo, porque el sensor no respondió).
/// </summary>
public record MonitoreoPanelDto(
    Guid PanelId,
    string CodigoPanel,
    EstadoSalud Estado,
    bool Activo,
    double? PotenciaActualW,
    double? TemperaturaCelsius,
    DateTime? UltimaLecturaUtc);