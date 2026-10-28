using SolarUY.Domain.Enums;

namespace SolarUY.Application.DTOs;

/// <summary>Datos necesarios para programar una tarea de mantenimiento sobre un panel.</summary>
public record CrearMantenimientoRequest(
    Guid PanelSolarId,
    TipoMantenimiento Tipo,
    DateTime FechaProgramada,
    string Descripcion,
    string? PersonalAsignado);
