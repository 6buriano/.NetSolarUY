namespace SolarUY.Application.DTOs;

/// <summary>Datos necesarios para dar de alta un parque solar.</summary>
public record CrearParqueRequest(
    string Nombre,
    string Ubicacion,
    double CapacidadInstaladaKW,
    DateTime FechaInstalacion);
