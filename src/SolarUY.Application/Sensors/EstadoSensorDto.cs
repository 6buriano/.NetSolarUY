namespace SolarUY.Application.Sensors;

/// <summary>Estado completo de un nodo según su sensor: las lecturas de todos sus paneles.</summary>
public record EstadoSensorDto(
    string IdentificadorHardwareARM,
    DateTime FechaHoraUtc,
    bool VolcadoRedActivo,

    /// <summary>
    /// Eficiencia de conversión en porcentaje (0 a 100). El sensor la reporta como fracción
    /// (0,97); la conversión la hace <c>SensorHttpClient</c>, así todo el sistema usa la misma
    /// unidad que el dominio.
    /// </summary>
    double EficienciaConversion,

    IReadOnlyList<LecturaSensorDto> Paneles);
