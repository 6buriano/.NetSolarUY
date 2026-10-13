using SolarUY.Application.DTOs;
using SolarUY.Domain.Enums;

namespace SolarUY.Web.Models;

/// <summary>Listado de paneles, con los filtros por nodo y por estado de salud.</summary>
public class PanelesIndexViewModel
{
    public IReadOnlyList<PanelDto> Paneles { get; set; } = [];

    public IReadOnlyList<NodoDto> Nodos { get; set; } = [];

    public Guid? NodoId { get; set; }

    public EstadoSalud? Estado { get; set; }
}
