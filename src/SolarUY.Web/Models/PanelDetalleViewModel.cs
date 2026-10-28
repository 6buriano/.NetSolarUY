using SolarUY.Application.DTOs;

namespace SolarUY.Web.Models;

/// <summary>
/// Pantalla de detalle de un panel: sus datos, su última lectura y la serie reciente que dejó el
/// colector. La serie puede estar vacía mientras el colector todavía no guardó nada.
/// </summary>
public class PanelDetalleViewModel
{
    public PanelDto Panel { get; set; } = null!;

    /// <summary>La lectura más reciente, o <c>null</c> si todavía no hay ninguna.</summary>
    public LecturaTelemetriaDto? UltimaLectura { get; set; }

    /// <summary>Las últimas lecturas, de la más reciente a la más antigua. Incluye a <see cref="UltimaLectura"/>.</summary>
    public IReadOnlyList<LecturaTelemetriaDto> LecturasRecientes { get; set; } = [];

    /// <summary>Historial de mantenimiento del panel, de la tarea más reciente a la más antigua.</summary>
    public IReadOnlyList<MantenimientoDto> Mantenimientos { get; set; } = [];
}
