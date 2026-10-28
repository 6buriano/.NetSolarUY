namespace SolarUY.Application.DTOs;

/// <summary>Generación de un panel en un período (día, mes o año), lista para mostrar en pantalla.</summary>
public record ResumenGeneracionDto(
    DateTime PeriodoInicio,
    string Etiqueta,
    int CantidadLecturas,
    double PotenciaPromedioW,
    double PotenciaMaximaW,
    double EnergiaEstimadaKWh);
