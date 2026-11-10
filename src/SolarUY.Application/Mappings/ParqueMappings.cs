using SolarUY.Application.DTOs;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;

namespace SolarUY.Application.Mappings;

/// <summary>Conversión de <see cref="ParqueSolar"/> a DTOs de la capa de aplicación.</summary>
public static class ParqueMappings
{
    public static ParqueDto ToDto(this ParqueSolar parque)
    {
        var paneles = parque.Nodos.SelectMany(nodo => nodo.Paneles).ToList();

        return new ParqueDto(
            parque.Id,
            parque.Nombre,
            parque.Ubicacion,
            parque.CapacidadInstaladaKW,
            parque.FechaInstalacion,
            parque.Nodos.Count,
            paneles.Count,
            paneles.Sum(panel => panel.PotenciaNominalW) / 1000d);
    }

    /// <summary>
    /// Resumen de un parque para responder "¿cómo está funcionando?": cuántos nodos y paneles tiene
    /// y cómo se reparten los paneles por estado. La suma de los cuatro conteos es
    /// <see cref="ResumenParqueDto.CantidadPaneles"/>, porque <see cref="EstadoSalud"/> tiene
    /// exactamente esos cuatro valores.
    /// </summary>
    public static ResumenParqueDto ToResumenDto(this ParqueSolar parque)
    {
        var paneles = parque.Nodos.SelectMany(nodo => nodo.Paneles).ToList();

        return new ResumenParqueDto(
            parque.Nombre,
            parque.Ubicacion,
            parque.Nodos.Count,
            paneles.Count,
            paneles.Count(panel => panel.Estado == EstadoSalud.OPERATIVO),
            paneles.Count(panel => panel.Estado == EstadoSalud.RENDIMIENTO_BAJO),
            paneles.Count(panel => panel.Estado == EstadoSalud.FALLA_DETECTADA),
            paneles.Count(panel => panel.Estado == EstadoSalud.DESCONECTADO));
    }
}
