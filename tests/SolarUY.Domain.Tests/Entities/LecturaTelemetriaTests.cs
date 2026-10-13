using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Tests.Entities;

public class LecturaTelemetriaTests
{
    private static readonly Guid PanelId = Guid.NewGuid();

    [Fact]
    public void Constructor_ConDatosValidos_AsignaIdYValores()
    {
        var fechaHora = new DateTime(2026, 6, 1, 12, 0, 0);

        var lectura = new LecturaTelemetria(PanelId, fechaHora, 8.5, 41.2, 350.2, 52.4);

        Assert.NotEqual(Guid.Empty, lectura.Id);
        Assert.Equal(PanelId, lectura.PanelSolarId);
        Assert.Equal(fechaHora, lectura.FechaHora);
        Assert.Equal(8.5, lectura.CorrienteContinua);
        Assert.Equal(41.2, lectura.Voltaje);
        Assert.Equal(350.2, lectura.PotenciaGeneradaWatts);
        Assert.Equal(52.4, lectura.TemperaturaCelsius);
    }

    [Fact]
    public void Constructor_SinEvaluar_ArrancaOperativo()
    {
        // El sensor sólo mide: la lectura nace OPERATIVO y el nodo la corrige al evaluarla.
        var lectura = new LecturaTelemetria(PanelId, DateTime.Now, 8.5, 41.2, 350.2, 52.4);

        Assert.Equal(EstadoSalud.OPERATIVO, lectura.EstadoDetectado);
    }

    [Fact]
    public void Constructor_SinPanel_LanzaReglaDeNegocio()
        => Assert.Throws<ReglaDeNegocioException>(
            () => new LecturaTelemetria(Guid.Empty, DateTime.Now, 8.5, 41.2, 350.2, 52.4));

    [Fact]
    public void Constructor_ConCorrienteNegativa_LanzaReglaDeNegocio()
        => Assert.Throws<ReglaDeNegocioException>(
            () => new LecturaTelemetria(PanelId, DateTime.Now, -1d, 41.2, 350.2, 52.4));

    [Fact]
    public void Constructor_ConVoltajeNegativo_LanzaReglaDeNegocio()
        => Assert.Throws<ReglaDeNegocioException>(
            () => new LecturaTelemetria(PanelId, DateTime.Now, 8.5, -1d, 350.2, 52.4));

    [Fact]
    public void Constructor_ConPotenciaNegativa_LanzaReglaDeNegocio()
        => Assert.Throws<ReglaDeNegocioException>(
            () => new LecturaTelemetria(PanelId, DateTime.Now, 8.5, 41.2, -1d, 52.4));

    [Theory]
    [InlineData(-60d)]
    [InlineData(200d)]
    public void Constructor_ConTemperaturaImposible_LanzaReglaDeNegocio(double temperatura)
        => Assert.Throws<ReglaDeNegocioException>(
            () => new LecturaTelemetria(PanelId, DateTime.Now, 8.5, 41.2, 350.2, temperatura));
}
