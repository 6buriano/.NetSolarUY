namespace SolarUY.Application.DTOs;

/// <summary>Datos del inversor asociado a un nodo, más el sensor ARM que tenga registrado.</summary>
public record InversorDto(
    Guid Id,
    string Modelo,
    string NumeroSerie,
    double EficienciaConversion,
    bool VolcadoRedActivo,
    SensorDispositivoDto? Sensor);
