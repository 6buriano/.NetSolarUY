namespace SolarUY.Application.DTOs;

/// <summary>Datos del inversor que se crea junto con el nodo.</summary>
public record InversorRequest(
    string Modelo,
    string NumeroSerie,
    double EficienciaConversion,
    bool VolcadoRedActivo);
