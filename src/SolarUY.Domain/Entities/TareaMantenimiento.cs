using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Entities;

/// <summary>
/// Tarea de mantenimiento programada sobre un panel solar.
/// </summary>
public class TareaMantenimiento
{
    public Guid Id { get; private set; }
    public Guid PanelSolarId { get; private set; }
    public TipoMantenimiento Tipo { get; private set; }
    public DateTime FechaProgramada { get; private set; }
    public DateTime? FechaRealizada { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public EstadoMantenimiento Estado { get; private set; }

    /// <summary>Campo que agrega el diagrama de clases por sobre specs.md.</summary>
    public string? PersonalAsignado { get; private set; }

    /// <summary>Constructor sin parámetros que exige Entity Framework Core para materializar.</summary>
    private TareaMantenimiento()
    {
    }

    public TareaMantenimiento(Guid panelSolarId, TipoMantenimiento tipo, DateTime fechaProgramada,
        string descripcion, string? personalAsignado)
    {
        if (panelSolarId == Guid.Empty)
            throw new ReglaDeNegocioException("La tarea de mantenimiento debe estar asociada a un panel solar.");

        if (!Enum.IsDefined(tipo))
            throw new ReglaDeNegocioException("El tipo de mantenimiento informado no es válido.");

        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ReglaDeNegocioException("La descripción de la tarea es obligatoria.");

        Id = Guid.NewGuid();
        PanelSolarId = panelSolarId;
        Tipo = tipo;
        FechaProgramada = fechaProgramada;
        Descripcion = descripcion.Trim();
        PersonalAsignado = string.IsNullOrWhiteSpace(personalAsignado) ? null : personalAsignado.Trim();
        Estado = EstadoMantenimiento.PENDIENTE;
    }

    public void Reprogramar(DateTime fechaProgramada, string descripcion, string? personalAsignado)
    {
        if (Estado == EstadoMantenimiento.COMPLETADO)
            throw new ReglaDeNegocioException("No se puede reprogramar una tarea de mantenimiento ya completada.");

        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ReglaDeNegocioException("La descripción de la tarea es obligatoria.");

        FechaProgramada = fechaProgramada;
        Descripcion = descripcion.Trim();
        PersonalAsignado = string.IsNullOrWhiteSpace(personalAsignado) ? null : personalAsignado.Trim();
    }

    public void Iniciar()
    {
        if (Estado != EstadoMantenimiento.PENDIENTE)
            throw new ReglaDeNegocioException("Solo una tarea pendiente puede pasar a en proceso.");

        Estado = EstadoMantenimiento.EN_PROCESO;
    }

    public void Completar(DateTime fechaRealizada)
    {
        if (Estado != EstadoMantenimiento.EN_PROCESO)
            throw new ReglaDeNegocioException("Solo una tarea en proceso puede completarse.");

        Estado = EstadoMantenimiento.COMPLETADO;
        FechaRealizada = fechaRealizada;
    }

    public void Cancelar()
    {
        if (Estado == EstadoMantenimiento.COMPLETADO)
            throw new ReglaDeNegocioException("No se puede cancelar una tarea de mantenimiento ya completada.");

        Estado = EstadoMantenimiento.CANCELADO;
    }
}
