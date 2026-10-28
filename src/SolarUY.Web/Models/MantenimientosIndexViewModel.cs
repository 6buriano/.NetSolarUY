using SolarUY.Application.DTOs;
using SolarUY.Domain.Enums;

namespace SolarUY.Web.Models;

/// <summary>Listado de tareas de mantenimiento, con filtro opcional por panel y por estado.</summary>
public class MantenimientosIndexViewModel
{
    public IReadOnlyList<MantenimientoDto> Tareas { get; set; } = [];

    public IReadOnlyList<PanelDto> Paneles { get; set; } = [];

    public Guid? PanelSolarId { get; set; }

    public EstadoMantenimiento? Estado { get; set; }
}
