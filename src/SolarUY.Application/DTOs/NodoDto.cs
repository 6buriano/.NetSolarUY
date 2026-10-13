namespace SolarUY.Application.DTOs;

/// <summary>Nodo de un parque solar, con su inversor y la cantidad de paneles asociados.</summary>
public record NodoDto(
    Guid Id,
    string Nombre,
    string UbicacionDentroParque,
    Guid ParqueSolarId,
    string? ParqueNombre,
    InversorDto Inversor,
    int CantidadPaneles);
