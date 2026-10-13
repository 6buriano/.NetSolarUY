using SolarUY.Domain.Enums;

namespace SolarUY.Application.DTOs;

/// <summary>Lectura de telemetría lista para mostrar en pantalla.</summary>
public record LecturaTelemetriaDto(
    Guid Id,
    Guid PanelSolarId,
    DateTime FechaHora,
    double CorrienteContinua,
    double Voltaje,
    double PotenciaGeneradaWatts,
    double TemperaturaCelsius,
    EstadoSalud EstadoDetectado);
