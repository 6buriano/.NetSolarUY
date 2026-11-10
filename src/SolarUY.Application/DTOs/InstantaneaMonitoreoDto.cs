namespace SolarUY.Application.DTOs;

/// <summary>"Foto" del estado actual de todos los parques, que se envía en vivo a las pantallas.</summary>
public record InstantaneaMonitoreoDto(
    DateTime GeneradaUtc,
    IReadOnlyList<MonitoreoParqueDto> Parques);