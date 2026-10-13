namespace SolarUY.Application.DTOs;

/// <summary>Datos necesarios para modificar un parque solar existente.</summary>
public record ActualizarParqueRequest(
    Guid Id,
    string Nombre,
    string Ubicacion,
    double CapacidadInstaladaKW,
    DateTime FechaInstalacion);
