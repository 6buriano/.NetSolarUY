using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Entities;

/// <summary>
/// Panel solar perteneciente a un nodo. Cuelga de él la telemetría y el mantenimiento.
/// </summary>
public class PanelSolar
{
    private const int LargoMaximoCodigo = 20;

    public Guid Id { get; private set; }

    /// <summary>
    /// Código con el que el sensor conoce a este panel (por ejemplo <c>P01</c>).
    /// Es lo que vincula el panel del sistema con el panel del hardware. Único dentro del nodo.
    /// </summary>
    public string CodigoPanel { get; private set; } = string.Empty;

    public string Modelo { get; private set; } = string.Empty;
    public string Fabricante { get; private set; } = string.Empty;
    public double PotenciaNominalW { get; private set; }
    public EstadoSalud Estado { get; private set; }

    /// <summary>
    /// Si el panel está habilitado en el hardware. Un panel inactivo no genera y el nodo lo
    /// diagnostica como <see cref="EstadoSalud.DESCONECTADO"/>.
    /// </summary>
    public bool Activo { get; private set; } = true;

    public DateTime FechaInstalacion { get; private set; }
    public Guid NodoId { get; private set; }
    public List<LecturaTelemetria> Lecturas { get; private set; } = [];
    public List<TareaMantenimiento> Mantenimientos { get; private set; } = [];

    /// <summary>Constructor sin parámetros que exige Entity Framework Core para materializar.</summary>
    private PanelSolar()
    {
    }

    public PanelSolar(string codigoPanel, string modelo, string fabricante, double potenciaNominalW,
        EstadoSalud estado, DateTime fechaInstalacion, Guid nodoId, bool activo = true)
    {
        Id = Guid.NewGuid();
        EstablecerDatos(codigoPanel, modelo, fabricante, potenciaNominalW, estado, fechaInstalacion, nodoId, activo);
    }

    public void Actualizar(string codigoPanel, string modelo, string fabricante, double potenciaNominalW,
        EstadoSalud estado, DateTime fechaInstalacion, Guid nodoId, bool activo)
        => EstablecerDatos(codigoPanel, modelo, fabricante, potenciaNominalW, estado, fechaInstalacion, nodoId, activo);

    /// <summary>La usa el colector cuando el sensor informa si el panel está habilitado.</summary>
    public void MarcarActividad(bool activo) => Activo = activo;

    /// <summary>La usa el colector cuando la telemetría detecta un cambio de salud.</summary>
    public void CambiarEstado(EstadoSalud estado) => Estado = estado;

    private void EstablecerDatos(string codigoPanel, string modelo, string fabricante, double potenciaNominalW,
        EstadoSalud estado, DateTime fechaInstalacion, Guid nodoId, bool activo)
    {
        if (string.IsNullOrWhiteSpace(codigoPanel))
            throw new ReglaDeNegocioException("El código de panel es obligatorio: es lo que lo identifica ante el sensor.");

        if (!CodigoValido(codigoPanel.Trim()))
        {
            throw new ReglaDeNegocioException(
                $"El código de panel admite hasta {LargoMaximoCodigo} caracteres: letras, números, '-' y '_'.");
        }

        if (string.IsNullOrWhiteSpace(modelo))
            throw new ReglaDeNegocioException("El modelo del panel es obligatorio.");

        if (string.IsNullOrWhiteSpace(fabricante))
            throw new ReglaDeNegocioException("El fabricante del panel es obligatorio.");

        if (potenciaNominalW <= 0)
            throw new ReglaDeNegocioException("La potencia nominal debe ser mayor a cero.");

        if (!Enum.IsDefined(estado))
            throw new ReglaDeNegocioException("El estado de salud informado no es válido.");

        if (nodoId == Guid.Empty)
            throw new ReglaDeNegocioException("El panel debe pertenecer a un nodo.");

        CodigoPanel = codigoPanel.Trim();
        Modelo = modelo.Trim();
        Fabricante = fabricante.Trim();
        PotenciaNominalW = potenciaNominalW;
        Estado = estado;
        FechaInstalacion = fechaInstalacion.Date;
        NodoId = nodoId;
        Activo = activo;
    }

    /// <summary>El código viaja en la URL del sensor, así que se limita a caracteres seguros.</summary>
    public static bool CodigoValido(string codigo)
        => codigo.Length is > 0 and <= LargoMaximoCodigo
           && codigo.All(caracter => char.IsAsciiLetterOrDigit(caracter) || caracter is '-' or '_');
}
