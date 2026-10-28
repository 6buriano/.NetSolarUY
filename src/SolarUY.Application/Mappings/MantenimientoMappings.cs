using SolarUY.Application.DTOs;
using SolarUY.Domain.Entities;

namespace SolarUY.Application.Mappings;

/// <summary>Conversión de <see cref="TareaMantenimiento"/> a DTO.</summary>
public static class MantenimientoMappings
{
    /// <param name="panelDescripcion">
    /// Se resuelve fuera del agregado, porque la tarea sólo guarda el identificador del panel.
    /// </param>
    public static MantenimientoDto ToDto(this TareaMantenimiento tarea, string? panelDescripcion)
        => new(
            tarea.Id,
            tarea.PanelSolarId,
            panelDescripcion,
            tarea.Tipo,
            tarea.FechaProgramada,
            tarea.FechaRealizada,
            tarea.Descripcion,
            tarea.Estado,
            tarea.PersonalAsignado);
}
