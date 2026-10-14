using System.ComponentModel.DataAnnotations;
using SolarUY.Domain.Entities;

namespace SolarUY.Web.Validation;

/// <summary>
/// Valida que el texto sea una dirección http o https absoluta, usando la misma regla que el
/// dominio (<see cref="SensorDispositivo.DireccionValida"/>), así el formulario y la entidad no
/// pueden discrepar.
/// </summary>
/// <remarks>
/// A propósito no se usa <c>[Url]</c> de DataAnnotations: esa regla activa la validación
/// <c>url</c> de jQuery Validation en el navegador, que exige un TLD o una IP pública y por lo
/// tanto rechaza <c>http://localhost:5100</c> y <c>http://127.0.0.1:5100</c>, que son justamente
/// las direcciones donde corre el simulador. El servidor, en cambio, las acepta.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DireccionHttpAttribute : ValidationAttribute
{
    public DireccionHttpAttribute()
    {
        ErrorMessage = "Tiene que ser una dirección http o https completa, por ejemplo http://localhost:5100";
    }

    public override bool IsValid(object? value)
    {
        // El campo vacío lo cubre [Required]; acá sólo se valida el formato.
        if (value is not string texto || string.IsNullOrWhiteSpace(texto))
        {
            return true;
        }

        return SensorDispositivo.DireccionValida(texto);
    }
}
