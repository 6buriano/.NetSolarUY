namespace SolarUY.Application.DTOs;

/// <summary>
/// Respuesta de una consulta en lenguaje natural. Con <see cref="Exito"/> en <c>false</c> no se pudo
/// responder (el proveedor de IA falló o se agotó el tiempo) y <see cref="Texto"/> trae el mensaje
/// para el usuario: la falla se modela como dato, no como excepción.
/// </summary>
public record ConsultaLenguajeNaturalResponse(bool Exito, string Texto);
