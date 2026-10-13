using System.ComponentModel.DataAnnotations;
using SolarUY.Application.DTOs;

namespace SolarUY.Web.Models;

/// <summary>Listado de nodos, con el filtro por parque solar.</summary>
public class NodosIndexViewModel
{
    public IReadOnlyList<NodoDto> Nodos { get; set; } = [];

    public IReadOnlyList<ParqueDto> Parques { get; set; } = [];

    [Display(Name = "Parque solar")]
    public Guid? ParqueSolarId { get; set; }
}
