using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Entities;

/// <summary>
/// Lectura de telemetría de un panel, tal como la mide el sensor.
/// El sensor sólo mide: el diagnóstico de <see cref="EstadoDetectado"/> lo hace el nodo en
/// <see cref="Nodo.EvaluarLecturas"/>, que es el único que ve a los paneles hermanos.
/// </summary>
public class LecturaTelemetria
{
    private const double TemperaturaMinimaPlausible = -50d;
    private const double TemperaturaMaximaPlausible = 150d;

    public Guid Id { get; private set; }
    public Guid PanelSolarId { get; private set; }
    public DateTime FechaHora { get; private set; }
    public double CorrienteContinua { get; private set; }
    public double Voltaje { get; private set; }
    public double PotenciaGeneradaWatts { get; private set; }
    public double TemperaturaCelsius { get; private set; }

    /// <summary>
    /// Arranca en <see cref="EstadoSalud.OPERATIVO"/> y lo corrige el nodo al evaluar la lectura.
    /// El colector nunca guarda una lectura sin evaluar.
    /// </summary>
    public EstadoSalud EstadoDetectado { get; private set; } = EstadoSalud.OPERATIVO;

    /// <summary>Constructor sin parámetros que exige Entity Framework Core para materializar.</summary>
    private LecturaTelemetria()
    {
    }

    public LecturaTelemetria(Guid panelSolarId, DateTime fechaHora, double corrienteContinua, double voltaje,
        double potenciaGeneradaWatts, double temperaturaCelsius)
    {
        if (panelSolarId == Guid.Empty)
            throw new ReglaDeNegocioException("La lectura debe estar asociada a un panel solar.");

        if (corrienteContinua < 0)
            throw new ReglaDeNegocioException("La corriente continua no puede ser negativa.");

        if (voltaje < 0)
            throw new ReglaDeNegocioException("El voltaje no puede ser negativo.");

        if (potenciaGeneradaWatts < 0)
            throw new ReglaDeNegocioException("La potencia generada no puede ser negativa.");

        if (temperaturaCelsius < TemperaturaMinimaPlausible || temperaturaCelsius > TemperaturaMaximaPlausible)
            throw new ReglaDeNegocioException("La temperatura informada está fuera de un rango físico plausible.");

        Id = Guid.NewGuid();
        PanelSolarId = panelSolarId;
        FechaHora = fechaHora;
        CorrienteContinua = corrienteContinua;
        Voltaje = voltaje;
        PotenciaGeneradaWatts = potenciaGeneradaWatts;
        TemperaturaCelsius = temperaturaCelsius;
    }

    /// <summary>La usa <see cref="Nodo.EvaluarLecturas"/> cuando diagnostica la lectura.</summary>
    internal void EstablecerEstadoDetectado(EstadoSalud estado)
    {
        if (!Enum.IsDefined(estado))
            throw new ReglaDeNegocioException("El estado detectado informado no es válido.");

        EstadoDetectado = estado;
    }
}
