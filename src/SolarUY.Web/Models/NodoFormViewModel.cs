using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SolarUY.Application.DTOs;
using SolarUY.Web.Validation;

namespace SolarUY.Web.Models;

/// <summary>
/// Formulario de alta y edición de un nodo. Los datos del inversor y del sensor ARM van planos
/// en el formulario porque un nodo siempre se crea con su inversor (regla de negocio 1) y con
/// el sensor que le da dirección al colector de telemetría.
/// </summary>
/// <remarks>
/// El sensor es opcional: un nodo se puede cargar antes de que su sensor esté conectado. Si se
/// completa el identificador de hardware, la dirección pasa a ser obligatoria; si no, el nodo
/// queda sin sensor.
/// </remarks>
public class NodoFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "El nombre del nodo es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ubicación dentro del parque es obligatoria.")]
    [StringLength(200, ErrorMessage = "La ubicación no puede superar los 200 caracteres.")]
    [Display(Name = "Ubicación dentro del parque")]
    public string UbicacionDentroParque { get; set; } = string.Empty;

    [Display(Name = "Parque solar")]
    public Guid ParqueSolarId { get; set; }

    [Required(ErrorMessage = "El modelo del inversor es obligatorio.")]
    [StringLength(100, ErrorMessage = "El modelo no puede superar los 100 caracteres.")]
    [Display(Name = "Modelo del inversor")]
    public string InversorModelo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de serie del inversor es obligatorio.")]
    [StringLength(100, ErrorMessage = "El número de serie no puede superar los 100 caracteres.")]
    [Display(Name = "Número de serie del inversor")]
    public string InversorNumeroSerie { get; set; } = string.Empty;

    [Range(0.01, 100, ErrorMessage = "La eficiencia debe estar entre 0 y 100 (porcentaje).")]
    [Display(Name = "Eficiencia de conversión (%)")]
    public double InversorEficienciaConversion { get; set; } = 97.5;

    [Display(Name = "Volcado a red activo")]
    public bool InversorVolcadoRedActivo { get; set; } = true;

    // Los tres campos del sensor son anulables a propósito: al ser tipos de referencia no anulables,
    // el binder los toma como obligatorios (con "The X field is required." en inglés) y no se podría
    // guardar un nodo sin sensor. Siendo anulables, la única regla es la de Validate().
    [StringLength(50, ErrorMessage = "El identificador no puede superar los 50 caracteres.")]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "El identificador sólo admite letras, números, '-' y '_'.")]
    [Display(Name = "Identificador de hardware (ARM)")]
    public string? IdentificadorHardwareARM { get; set; }

    [StringLength(50, ErrorMessage = "La versión de firmware no puede superar los 50 caracteres.")]
    [Display(Name = "Versión de firmware del sensor")]
    public string? SensorVersionFirmware { get; set; }

    [StringLength(300, ErrorMessage = "La dirección no puede superar los 300 caracteres.")]
    // Se valida con la regla del dominio y no con [Url], que rechaza localhost y las IP internas.
    [DireccionHttp]
    [Display(Name = "Dirección del sensor")]
    public string? SensorUrlBase { get; set; } = "http://localhost:5100";

    [Display(Name = "Sensor activo")]
    public bool SensorActivo { get; set; } = true;

    /// <summary>Si el formulario trae un identificador, el nodo lleva sensor.</summary>
    public bool TieneSensor => !string.IsNullOrWhiteSpace(IdentificadorHardwareARM);

    /// <summary>
    /// Mensaje que aparece cuando el identificador cargado no existe en el simulador. Si tiene
    /// texto, el formulario ofrece las dos salidas: crear el sensor o crear el nodo sin él.
    /// </summary>
    public string? AvisoSensorInexistente { get; set; }

    /// <summary>Qué eligió el usuario en el aviso anterior. Lo manda el botón que apretó.</summary>
    public string? AccionArm { get; set; }

    /// <summary>Parques disponibles para el desplegable. No se postea, lo llena el controlador.</summary>
    public IReadOnlyList<ParqueDto> Parques { get; set; } = [];

    /// <summary>Valores posibles de <see cref="AccionArm"/>.</summary>
    public static class AccionesArm
    {
        /// <summary>El usuario quiere que el simulador empiece a emular el sensor que escribió.</summary>
        public const string CrearEnHardware = "CrearEnHardware";

        /// <summary>El usuario prefiere crear el nodo sin sensor.</summary>
        public const string SinSensor = "SinSensor";
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(IdentificadorHardwareARM))
        {
            // Sin identificador no hay sensor, y la dirección queda ignorada.
            yield break;
        }

        if (string.IsNullOrWhiteSpace(SensorUrlBase))
        {
            yield return new ValidationResult(
                "Si cargás el identificador del sensor, la dirección es obligatoria: es a donde el colector le pide la telemetría.",
                [nameof(SensorUrlBase)]);
        }
    }
}
