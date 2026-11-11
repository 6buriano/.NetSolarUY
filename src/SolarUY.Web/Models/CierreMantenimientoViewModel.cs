using System.ComponentModel.DataAnnotations;
using SolarUY.Domain.Entities;

namespace SolarUY.Web.Models;

/// <summary>
/// Lo que se pide al completar una tarea: cuándo se terminó el trabajo (la hora la informa quien lo
/// hizo, porque puede marcarla más tarde) y, si se quiere, qué se hizo.
/// </summary>
public class CierreMantenimientoViewModel
{
    [Required(ErrorMessage = "La fecha y hora de realización son obligatorias.")]
    [Display(Name = "Fecha y hora de realización")]
    public DateTime FechaRealizada { get; set; }

    [StringLength(TareaMantenimiento.LargoMaximoObservaciones,
        ErrorMessage = "Las observaciones no pueden superar los {1} caracteres.")]
    [Display(Name = "Observaciones del trabajo realizado")]
    public string? ObservacionesCierre { get; set; }
}
