using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Entities;

/// <summary>
/// Sensor físico basado en ARM que reporta la telemetría de un inversor.
/// </summary>
public class SensorDispositivo
{
    public Guid Id { get; private set; }
    public string IdentificadorHardwareARM { get; private set; } = string.Empty;
    public string VersionFirmware { get; private set; } = string.Empty;
    public bool Activo { get; private set; }
    public Guid InversorId { get; private set; }

    /// <summary>
    /// Dirección base del sensor, por ejemplo <c>http://localhost:5100</c>. Es lo que permite
    /// que el colector lo consulte por HTTP como si fuera hardware real. En un parque de verdad
    /// cada sensor tendría su propia dirección.
    /// </summary>
    public string UrlBase { get; private set; } = string.Empty;

    /// <summary>Constructor sin parámetros que exige Entity Framework Core para materializar.</summary>
    private SensorDispositivo()
    {
    }

    public SensorDispositivo(string identificadorHardwareARM, string versionFirmware, bool activo, string urlBase)
    {
        Id = Guid.NewGuid();
        EstablecerDatos(identificadorHardwareARM, versionFirmware, activo, urlBase);
    }

    public void Actualizar(string identificadorHardwareARM, string versionFirmware, bool activo, string urlBase)
        => EstablecerDatos(identificadorHardwareARM, versionFirmware, activo, urlBase);

    public void Activar() => Activo = true;

    public void Desactivar() => Activo = false;

    /// <summary>La usa <see cref="Inversor"/> al registrar el sensor para mantener la relación 1 a 1.</summary>
    internal void AsignarAInversor(Guid inversorId) => InversorId = inversorId;

    /// <summary>
    /// Indica si el texto es una dirección http o https absoluta. Acepta <c>localhost</c> y
    /// direcciones de red interna, que es donde vive el simulador.
    /// La usa también el formulario del ABM, para no validar con una regla más estricta que esta:
    /// la regla <c>url</c> de jQuery Validation (la que activa <c>[Url]</c> de DataAnnotations)
    /// exige un TLD o una IP pública y rechaza <c>http://localhost:5100</c>.
    /// </summary>
    public static bool DireccionValida(string? urlBase)
    {
        var url = (urlBase ?? string.Empty).Trim().TrimEnd('/');

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private void EstablecerDatos(string identificadorHardwareARM, string versionFirmware, bool activo, string urlBase)
    {
        if (string.IsNullOrWhiteSpace(identificadorHardwareARM))
            throw new ReglaDeNegocioException("El identificador de hardware del sensor es obligatorio.");

        var url = (urlBase ?? string.Empty).Trim().TrimEnd('/');
        if (!DireccionValida(url))
        {
            throw new ReglaDeNegocioException(
                "La dirección del sensor debe ser una URL http o https absoluta (por ejemplo http://localhost:5100).");
        }

        IdentificadorHardwareARM = identificadorHardwareARM.Trim();
        VersionFirmware = (versionFirmware ?? string.Empty).Trim();
        Activo = activo;
        UrlBase = url;
    }
}
