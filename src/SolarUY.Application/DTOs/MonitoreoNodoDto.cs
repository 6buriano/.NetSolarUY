namespace SolarUY.Application.DTOs;

/// <summary>Potencia actual de un nodo: la suma de sus paneles activos con lectura reciente.</summary>
public record MonitoreoNodoDto(
    Guid NodoId,
    string Nombre,
    double PotenciaActualW,
    IReadOnlyList<MonitoreoPanelDto> Paneles);