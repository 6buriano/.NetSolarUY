namespace SolarUY.Application.DTOs;

/// <summary>
/// Pregunta del administrador en lenguaje natural. Cada envío es independiente: no lleva historial
/// ni identificador de conversación, y la valida y recorta <c>ConsultaService</c>.
/// </summary>
public record ConsultaLenguajeNaturalRequest(string Pregunta);
