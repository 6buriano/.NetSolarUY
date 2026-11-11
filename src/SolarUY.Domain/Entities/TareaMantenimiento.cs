using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Entities;

/// <summary>
/// Tarea de mantenimiento programada sobre un panel solar.
/// </summary>
public class TareaMantenimiento
{
    /// <summary>Largo máximo de las observaciones que se anotan al completar la tarea.</summary>
    public const int LargoMaximoObservaciones = 500;

    public Guid Id { get; private set; }
    public Guid PanelSolarId { get; private set; }
    public TipoMantenimiento Tipo { get; private set; }
    public DateTime FechaProgramada { get; private set; }
    public DateTime? FechaRealizada { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public EstadoMantenimiento Estado { get; private set; }

    /// <summary>Campo que agrega el diagrama de clases por sobre specs.md.</summary>
    public string? PersonalAsignado { get; private set; }

    /// <summary>
    /// Qué se hizo, anotado al completar la tarea. Queda como registro del trabajo y no como una
    /// simple marca de tiempo.
    /// </summary>
    public string? ObservacionesCierre { get; private set; }

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

    /// <summary>
    /// Completa la tarea. La fecha de realización la informa quien hizo el trabajo (no es la del
    /// servidor): si el técnico la marca más tarde, la hora del sistema no coincidiría con la real.
    /// </summary>
    /// <param name="fechaRealizada">Cuándo se terminó el trabajo, en UTC.</param>
    /// <param name="observaciones">Qué se hizo; opcional.</param>
    /// <param name="ahoraUtc">Momento actual, para rechazar una fecha de realización futura.</param>
    public void Completar(DateTime fechaRealizada, string? observaciones, DateTime ahoraUtc)
    {
        if (Estado != EstadoMantenimiento.EN_PROCESO)
            throw new ReglaDeNegocioException("Solo una tarea en proceso puede completarse.");

        if (fechaRealizada == default)
            throw new ReglaDeNegocioException("La fecha de realización es obligatoria.");

        if (fechaRealizada > ahoraUtc)
            throw new ReglaDeNegocioException("La fecha de realización no puede ser posterior al momento actual.");

        if (observaciones is not null && observaciones.Trim().Length > LargoMaximoObservaciones)
            throw new ReglaDeNegocioException(
                $"Las observaciones del cierre no pueden superar los {LargoMaximoObservaciones} caracteres.");

        Estado = EstadoMantenimiento.COMPLETADO;
        FechaRealizada = fechaRealizada;
        ObservacionesCierre = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
    }

    public void Cancelar()
    {
        if (Estado == EstadoMantenimiento.COMPLETADO)
            throw new ReglaDeNegocioException("No se puede cancelar una tarea de mantenimiento ya completada.");

        Estado = EstadoMantenimiento.CANCELADO;
    }
}
