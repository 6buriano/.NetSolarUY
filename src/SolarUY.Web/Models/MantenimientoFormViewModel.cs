using System.ComponentModel.DataAnnotations;
using SolarUY.Application.DTOs;
using SolarUY.Domain.Enums;

namespace SolarUY.Web.Models;

/// <summary>
/// Formulario de alta y reprogramación de una tarea de mantenimiento. Si <see cref="Id"/> viene
/// con valor, la vista está reprogramando (no se puede cambiar de panel ni de tipo); si viene
/// nulo, está creando.
/// </summary>
public class MantenimientoFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Elegí el panel sobre el que se programa la tarea.")]
    [Display(Name = "Panel")]
    public Guid PanelSolarId { get; set; }

    [Display(Name = "Tipo de tarea")]
    public TipoMantenimiento Tipo { get; set; } = TipoMantenimiento.INSPECCION_PREVENTIVA;

    [Required(ErrorMessage = "La fecha programada es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha programada")]
    public DateTime FechaProgramada { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "El personal asignado no puede superar los 150 caracteres.")]
    [Display(Name = "Personal asignado")]
    public string? PersonalAsignado { get; set; }

    /// <summary>Paneles disponibles para el desplegable. No se postea, lo llena el controlador.</summary>
    public IReadOnlyList<PanelDto> Paneles { get; set; } = [];
}
