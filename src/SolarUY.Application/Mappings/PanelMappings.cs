using SolarUY.Application.DTOs;
using SolarUY.Domain.Entities;

namespace SolarUY.Application.Mappings;

/// <summary>Conversión de <see cref="PanelSolar"/> y <see cref="LecturaTelemetria"/> a DTOs.</summary>
public static class PanelMappings
{
    /// <param name="nodoNombre">
    /// Se resuelve fuera del agregado, porque el panel sólo guarda el identificador del nodo.
    /// </param>
    public static PanelDto ToDto(this PanelSolar panel, string? nodoNombre)
        => new(
            panel.Id,
            panel.CodigoPanel,
            panel.Modelo,
            panel.Fabricante,
            panel.PotenciaNominalW,
            panel.Estado,
            panel.Activo,
            panel.FechaInstalacion,
            panel.NodoId,
            nodoNombre);

    public static LecturaTelemetriaDto ToDto(this LecturaTelemetria lectura)
        => new(
            lectura.Id,
            lectura.PanelSolarId,
            lectura.FechaHora,
            lectura.CorrienteContinua,
            lectura.Voltaje,
            lectura.PotenciaGeneradaWatts,
            lectura.TemperaturaCelsius,
            lectura.EstadoDetectado);
}
