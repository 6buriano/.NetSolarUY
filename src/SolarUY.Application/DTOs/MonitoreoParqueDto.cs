namespace SolarUY.Application.DTOs;

/// <summary>Potencia actual de un parque: la suma de sus nodos.</summary>
public record MonitoreoParqueDto(
    Guid ParqueId,
    string Nombre,
    double PotenciaActualW,
    IReadOnlyList<MonitoreoNodoDto> Nodos);