using System.ComponentModel.DataAnnotations;

namespace SolarUY.Web.Models;

/// <summary>
/// Formulario de alta y edición de un parque solar. Si <see cref="Id"/> viene con valor,
/// la vista está editando; si viene nulo, está creando.
/// </summary>
public class ParqueFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "El nombre del parque es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    [StringLength(200, ErrorMessage = "La ubicación no puede superar los 200 caracteres.")]
    [Display(Name = "Ubicación")]
    public string Ubicacion { get; set; } = string.Empty;

    [Range(0.01, 1_000_000, ErrorMessage = "La capacidad instalada debe ser mayor a cero.")]
    [Display(Name = "Capacidad instalada (kW)")]
    public double CapacidadInstaladaKW { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de instalación")]
    public DateTime FechaInstalacion { get; set; } = DateTime.Today;
}
