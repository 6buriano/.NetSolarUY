using SolarUY.Application.DTOs;

namespace SolarUY.Web.Models;

/// <summary>Pantalla de detalle de un nodo: sus datos, su inversor y la lista de paneles.</summary>
public class NodoDetalleViewModel
{
    public NodoDto Nodo { get; set; } = null!;

    public IReadOnlyList<PanelDto> Paneles { get; set; } = [];
}
