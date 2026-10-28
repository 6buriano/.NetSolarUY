namespace SolarUY.Domain.Repositories;

/// <summary>
/// Potencia agregada de un panel dentro de un período (día, mes o año), tal como la devuelve
/// <see cref="ITelemetriaRepository.ObtenerAgregadoPorPanelAsync"/>. Es un dato crudo de
/// persistencia: la capa de aplicación lo convierte al DTO que muestra la pantalla.
/// </summary>
public sealed record AgregadoPotencia(
    DateTime PeriodoInicio,
    int CantidadLecturas,
    double PotenciaPromedioW,
    double PotenciaMaximaW,
    double PotenciaSumaW);
