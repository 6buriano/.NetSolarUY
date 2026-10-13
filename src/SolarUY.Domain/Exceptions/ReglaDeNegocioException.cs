namespace SolarUY.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una operación viola una regla de negocio del dominio.
/// La capa de presentación la captura para mostrar el mensaje al usuario.
/// </summary>
public class ReglaDeNegocioException : Exception
{
    public ReglaDeNegocioException(string mensaje) : base(mensaje)
    {
    }

    public ReglaDeNegocioException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
