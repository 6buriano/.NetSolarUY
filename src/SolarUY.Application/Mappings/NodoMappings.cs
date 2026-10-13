using SolarUY.Application.DTOs;
using SolarUY.Domain.Entities;

namespace SolarUY.Application.Mappings;

/// <summary>Conversión de <see cref="Nodo"/>, <see cref="Inversor"/> y <see cref="SensorDispositivo"/> a DTOs.</summary>
public static class NodoMappings
{
    /// <param name="parqueNombre">
    /// Se resuelve fuera del agregado, porque el nodo sólo guarda el identificador del parque.
    /// </param>
    public static NodoDto ToDto(this Nodo nodo, string? parqueNombre)
        => new(
            nodo.Id,
            nodo.Nombre,
            nodo.UbicacionDentroParque,
            nodo.ParqueSolarId,
            parqueNombre,
            nodo.Inversor.ToDto(),
            nodo.Paneles.Count);

    public static InversorDto ToDto(this Inversor inversor)
        => new(
            inversor.Id,
            inversor.Modelo,
            inversor.NumeroSerie,
            inversor.EficienciaConversion,
            inversor.VolcadoRedActivo,
            inversor.Sensor?.ToDto());

    public static SensorDispositivoDto ToDto(this SensorDispositivo sensor)
        => new(
            sensor.Id,
            sensor.IdentificadorHardwareARM,
            sensor.VersionFirmware,
            sensor.Activo,
            sensor.UrlBase);
}
