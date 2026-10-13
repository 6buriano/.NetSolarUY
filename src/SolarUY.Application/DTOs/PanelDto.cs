using SolarUY.Domain.Enums;

namespace SolarUY.Application.DTOs;

/// <summary>Panel solar listo para mostrar en pantalla.</summary>
public record PanelDto(
    Guid Id,
    string CodigoPanel,
    string Modelo,
    string Fabricante,
    double PotenciaNominalW,
    EstadoSalud Estado,
    bool Activo,
    DateTime FechaInstalacion,
    Guid NodoId,
    string? NodoNombre);
