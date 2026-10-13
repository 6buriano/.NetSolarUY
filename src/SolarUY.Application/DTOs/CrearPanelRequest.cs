using SolarUY.Domain.Enums;

namespace SolarUY.Application.DTOs;

/// <summary>Datos necesarios para dar de alta un panel solar.</summary>
public record CrearPanelRequest(
    string CodigoPanel,
    string Modelo,
    string Fabricante,
    double PotenciaNominalW,
    EstadoSalud Estado,
    bool Activo,
    DateTime FechaInstalacion,
    Guid NodoId);
