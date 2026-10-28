using System.ComponentModel.DataAnnotations;
using SolarUY.Application.DTOs;
using SolarUY.Domain.Enums;

namespace SolarUY.Web.Models;

/// <summary>Pantalla de consultas históricas: generación de un panel agrupada por día, mes o año.</summary>
public class HistoricoIndexViewModel
{
    public IReadOnlyList<PanelDto> Paneles { get; set; } = [];

    public Guid? PanelSolarId { get; set; }

    public GranularidadConsulta Granularidad { get; set; } = GranularidadConsulta.DIA;

    [DataType(DataType.Date)]
    public DateTime? Desde { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Hasta { get; set; }

    public IReadOnlyList<ResumenGeneracionDto> Resultados { get; set; } = [];
}
