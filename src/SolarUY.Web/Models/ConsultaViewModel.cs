using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SolarUY.Application.Services;

namespace SolarUY.Web.Models;

/// <summary>
/// Formulario y resultado de la pantalla Consultas. Lleva las dos cosas en el mismo modelo porque
/// la acción no redirige: después del POST se vuelve a mostrar la misma vista con la respuesta.
/// </summary>
public class ConsultaViewModel
{
    [Required(ErrorMessage = "Escribí una pregunta.")]
    [StringLength(ConsultaService.LargoMaximoPregunta,
        ErrorMessage = "La pregunta no puede superar los 500 caracteres.")]
    [Display(Name = "Pregunta")]
    public string Pregunta { get; set; } = string.Empty;

    // Las tres propiedades de abajo son SALIDA, no entrada del formulario: [BindNever] impide que el
    // model binder las complete desde el POST. Sin esto, alguien podría mandar Exito=true con un
    // texto inventado y la vista lo mostraría como si fuera la respuesta del agente.

    /// <summary>La pregunta que se respondió, ya recortada. Se muestra arriba de la respuesta.</summary>
    [BindNever]
    public string? PreguntaEnviada { get; set; }

    /// <summary>Respuesta del agente, o el mensaje de falla si no se pudo responder.</summary>
    [BindNever]
    public string? Respuesta { get; set; }

    /// <summary>Falso cuando el agente no pudo responder (sin clave, sin red, cuota agotada o timeout).</summary>
    [BindNever]
    public bool Exito { get; set; }
}
