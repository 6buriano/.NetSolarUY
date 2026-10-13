namespace SolarUY.Application.DTOs;

/// <summary>Parque solar listo para mostrar en pantalla, con los totales que necesita el listado.</summary>
public record ParqueDto(
    Guid Id,
    string Nombre,
    string Ubicacion,
    double CapacidadInstaladaKW,
    DateTime FechaInstalacion,
    int CantidadNodos,
    int CantidadPaneles,
    double PotenciaNominalPanelesKW);
