using SolarUY.Application.DTOs;
using SolarUY.Domain.Entities;

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
}
