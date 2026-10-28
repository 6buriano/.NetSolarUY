using SolarUY.Domain.Enums;

namespace SolarUY.Application.DTOs;

/// <summary>Tarea de mantenimiento lista para mostrar en pantalla.</summary>
public record MantenimientoDto(
    Guid Id,
    Guid PanelSolarId,
    string? PanelDescripcion,
    TipoMantenimiento Tipo,
    DateTime FechaProgramada,
    DateTime? FechaRealizada,
    string Descripcion,
    EstadoMantenimiento Estado,
    string? PersonalAsignado);
