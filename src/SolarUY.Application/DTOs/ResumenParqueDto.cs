namespace SolarUY.Application.DTOs;

/// <summary>
/// Resumen de un parque para responder "¿cómo está funcionando?": cuántos nodos y paneles tiene y
/// cómo se reparten los paneles por estado, sin entrar al detalle de cada uno. Los cuatro conteos
/// de estado suman <see cref="CantidadPaneles"/>.
/// </summary>
public record ResumenParqueDto(
    string Nombre,
    string Ubicacion,
    int CantidadNodos,
    int CantidadPaneles,
    int PanelesOperativos,
    int PanelesRendimientoBajo,
    int PanelesEnFalla,
    int PanelesDesconectados);
