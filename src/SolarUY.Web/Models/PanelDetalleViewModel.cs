using SolarUY.Application.DTOs;

namespace SolarUY.Web.Models;

/// <summary>
/// Pantalla de detalle de un panel. La última lectura viene del simulador, así que puede
/// estar vacía mientras el simulador todavía no envió datos.
/// </summary>
public class PanelDetalleViewModel
{
    public PanelDto Panel { get; set; } = null!;

    public LecturaTelemetriaDto? UltimaLectura { get; set; }
}
