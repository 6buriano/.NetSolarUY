using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Entities;

/// <summary>
/// Inversor asociado a un nodo. La eficiencia de conversión se expresa en porcentaje (0 a 100).
/// </summary>
public class Inversor
{
    public Guid Id { get; private set; }
    public string Modelo { get; private set; } = string.Empty;
    public string NumeroSerie { get; private set; } = string.Empty;
    public double EficienciaConversion { get; private set; }
    public bool VolcadoRedActivo { get; private set; }
    public Guid NodoId { get; private set; }

    /// <summary>El sensor ARM se registra recién cuando el dispositivo se conecta, por eso puede ser nulo.</summary>
    public SensorDispositivo? Sensor { get; private set; }

    /// <summary>Constructor sin parámetros que exige Entity Framework Core para materializar.</summary>
    private Inversor()
    {
    }

    public Inversor(string modelo, string numeroSerie, double eficienciaConversion, bool volcadoRedActivo)
    {
        Id = Guid.NewGuid();
        EstablecerDatos(modelo, numeroSerie, eficienciaConversion, volcadoRedActivo);
    }

    public void Actualizar(string modelo, string numeroSerie, double eficienciaConversion, bool volcadoRedActivo)
        => EstablecerDatos(modelo, numeroSerie, eficienciaConversion, volcadoRedActivo);

    /// <summary>La usa <see cref="Nodo"/> al construirse para mantener la relación 1 a 1.</summary>
    internal void AsignarANodo(Guid nodoId) => NodoId = nodoId;

    /// <summary>La usa el colector cuando el sensor informa si el volcado a la red está activo.</summary>
    public void EstablecerVolcadoRed(bool activo) => VolcadoRedActivo = activo;

    public void RegistrarSensor(SensorDispositivo sensor)
    {
        if (sensor is null)
        {
            throw new ReglaDeNegocioException("El sensor a registrar no puede ser nulo.");
        }

        // Mantiene la relación 1 a 1: el sensor guarda a qué inversor pertenece.
        sensor.AsignarAInversor(Id);
        Sensor = sensor;
    }

    /// <summary>
    /// Desvincula el sensor del inversor. Como el sensor no existe sin su inversor, se elimina al
    /// guardar los cambios.
    /// </summary>
    public void QuitarSensor() => Sensor = null;

    private void EstablecerDatos(string modelo, string numeroSerie, double eficienciaConversion, bool volcadoRedActivo)
    {
        if (string.IsNullOrWhiteSpace(modelo))
            throw new ReglaDeNegocioException("El modelo del inversor es obligatorio.");

        if (string.IsNullOrWhiteSpace(numeroSerie))
            throw new ReglaDeNegocioException("El número de serie del inversor es obligatorio.");

        if (eficienciaConversion <= 0 || eficienciaConversion > 100)
            throw new ReglaDeNegocioException("La eficiencia de conversión debe ser mayor a 0 y hasta 100 (porcentaje).");

        Modelo = modelo.Trim();
        NumeroSerie = numeroSerie.Trim();
        EficienciaConversion = eficienciaConversion;
        VolcadoRedActivo = volcadoRedActivo;
    }
}
