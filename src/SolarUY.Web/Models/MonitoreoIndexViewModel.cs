using SolarUY.Application.DTOs;

namespace SolarUY.Web.Models;

/// <summary>Datos de la pantalla de Monitoreo: la foto completa y el parque que se está mirando.</summary>
public class MonitoreoIndexViewModel
{
    public required InstantaneaMonitoreoDto Instantanea { get; init; }

    /// <summary>Parque elegido en el combo (o el primero). Nulo si no hay parques cargados.</summary>
    public MonitoreoParqueDto? Parque { get; init; }
}