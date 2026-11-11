namespace SolarUY.Application.DTOs;

/// <summary>
/// Generación estimada de un parque o de todo el sistema en un período (día, mes o año), lista para
/// mostrar en pantalla o para que la use el agente. No se reutiliza <see cref="ResumenGeneracionDto"/>
/// porque su potencia promedio y máxima son de un panel y confundirían al modelo.
/// </summary>
public record GeneracionPeriodoDto(
    string Etiqueta,
    int CantidadLecturas,
    double EnergiaEstimadaKWh);
