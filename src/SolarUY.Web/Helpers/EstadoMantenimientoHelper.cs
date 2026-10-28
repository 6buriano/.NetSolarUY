using SolarUY.Domain.Enums;

namespace SolarUY.Web.Helpers;

/// <summary>Traduce el estado de una tarea de mantenimiento a una clase de Bootstrap.</summary>
public static class EstadoMantenimientoHelper
{
    public static string ClaseBadge(EstadoMantenimiento estado) => estado switch
    {
        EstadoMantenimiento.PENDIENTE => "text-bg-secondary",
        EstadoMantenimiento.EN_PROCESO => "text-bg-warning",
        EstadoMantenimiento.COMPLETADO => "text-bg-success",
        EstadoMantenimiento.CANCELADO => "text-bg-danger",
        _ => "text-bg-secondary"
    };
}
