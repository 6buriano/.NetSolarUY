using SolarUY.Domain.Enums;

namespace SolarUY.Web.Helpers;

/// <summary>
/// Traduce valores del dominio a cosas que entiende la vista (clases de Bootstrap).
/// </summary>
public static class EstadoSaludHelper
{
    public static string ClaseBadge(EstadoSalud estado) => estado switch
    {
        EstadoSalud.OPERATIVO => "text-bg-success",
        EstadoSalud.RENDIMIENTO_BAJO => "text-bg-warning",
        EstadoSalud.FALLA_DETECTADA => "text-bg-danger",
        EstadoSalud.DESCONECTADO => "text-bg-secondary",
        _ => "text-bg-secondary"
    };
}
