using SolarUY.Domain.Enums;

namespace SolarUY.Application.DTOs;

/// <summary>Datos necesarios para modificar un panel solar existente.</summary>
public record ActualizarPanelRequest(
    Guid Id,
    string CodigoPanel,
    string Modelo,
    string Fabricante,
    double PotenciaNominalW,
    EstadoSalud Estado,
    bool Activo,
    DateTime FechaInstalacion,
    Guid NodoId);
