using System.ComponentModel.DataAnnotations;
using SolarUY.Application.DTOs;
using SolarUY.Domain.Enums;

namespace SolarUY.Web.Models;

/// <summary>
/// Formulario de alta y edición de un panel solar. El código de panel es el vínculo con el
/// panel del sensor, así que es obligatorio y único dentro del nodo.
/// </summary>
public class PanelFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "El código de panel es obligatorio: es lo que lo identifica ante el sensor.")]
    [StringLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "El código sólo admite letras, números, '-' y '_'.")]
    [Display(Name = "Código de panel")]
    public string CodigoPanel { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo del panel es obligatorio.")]
    [StringLength(100, ErrorMessage = "El modelo no puede superar los 100 caracteres.")]
    [Display(Name = "Modelo")]
    public string Modelo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El fabricante es obligatorio.")]
    [StringLength(100, ErrorMessage = "El fabricante no puede superar los 100 caracteres.")]
    [Display(Name = "Fabricante")]
    public string Fabricante { get; set; } = string.Empty;

    [Range(0.01, 100_000, ErrorMessage = "La potencia nominal debe ser mayor a cero.")]
    [Display(Name = "Potencia nominal (W)")]
    public double PotenciaNominalW { get; set; } = 550;

    [Display(Name = "Estado de salud")]
    public EstadoSalud Estado { get; set; } = EstadoSalud.OPERATIVO;

    [Display(Name = "Panel activo")]
    public bool Activo { get; set; } = true;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de instalación")]
    public DateTime FechaInstalacion { get; set; } = DateTime.Today;

    [Display(Name = "Nodo")]
    public Guid NodoId { get; set; }

    /// <summary>Nodos disponibles para el desplegable. No se postea, lo llena el controlador.</summary>
    public IReadOnlyList<NodoDto> Nodos { get; set; } = [];
}
