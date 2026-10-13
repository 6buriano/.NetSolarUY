using SolarUY.Application.DTOs;

namespace SolarUY.Web.Models;

/// <summary>Pantalla de detalle de un parque: sus datos y la lista de nodos que contiene.</summary>
public class ParqueDetalleViewModel
{
    public ParqueDto Parque { get; set; } = null!;

    public IReadOnlyList<NodoDto> Nodos { get; set; } = [];
}
