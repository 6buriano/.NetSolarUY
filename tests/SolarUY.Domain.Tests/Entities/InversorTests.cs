using SolarUY.Domain.Entities;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Tests.Entities;

public class InversorTests
{
    [Fact]
    public void Constructor_ConDatosValidos_AsignaIdYPropiedades()
    {
        var inversor = new Inversor("  Huawei SUN2000-5KTL  ", " SN-000123 ", 97.5, true);

        Assert.NotEqual(Guid.Empty, inversor.Id);
        Assert.Equal("Huawei SUN2000-5KTL", inversor.Modelo);
        Assert.Equal("SN-000123", inversor.NumeroSerie);
        Assert.Equal(97.5, inversor.EficienciaConversion);
        Assert.True(inversor.VolcadoRedActivo);
        Assert.Null(inversor.Sensor);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-5d)]
    [InlineData(100.01d)]
    public void Constructor_ConEficienciaFueraDeRango_LanzaReglaDeNegocio(double eficiencia)
        => Assert.Throws<ReglaDeNegocioException>(
            () => new Inversor("Huawei SUN2000-5KTL", "SN-000123", eficiencia, true));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_SinNumeroDeSerie_LanzaReglaDeNegocio(string numeroSerie)
        => Assert.Throws<ReglaDeNegocioException>(() => new Inversor("Huawei", numeroSerie, 97.5, true));

    [Fact]
    public void RegistrarSensor_AsociaElSensorAlInversor()
    {
        var inversor = new Inversor("Huawei SUN2000-5KTL", "SN-000123", 97.5, true);
        var sensor = new SensorDispositivo("ARM-ESP32-001", "1.4.2", true, "http://localhost:5100");

        inversor.RegistrarSensor(sensor);

        Assert.Same(sensor, inversor.Sensor);
        Assert.Equal(inversor.Id, sensor.InversorId);
    }

    [Fact]
    public void EstablecerVolcadoRed_CambiaElFlag()
    {
        var inversor = new Inversor("Huawei SUN2000-5KTL", "SN-000123", 97.5, true);

        // Lo usa el colector cuando el sensor informa el estado del volcado a la red.
        inversor.EstablecerVolcadoRed(false);
        Assert.False(inversor.VolcadoRedActivo);

        inversor.EstablecerVolcadoRed(true);
        Assert.True(inversor.VolcadoRedActivo);
    }

    [Fact]
    public void Actualizar_CambiaLosDatosDelInversor()
    {
        var inversor = new Inversor("Huawei SUN2000-5KTL", "SN-000123", 97.5, true);

        inversor.Actualizar("Growatt MOD 5000TL3-X", "SN-999999", 98.2, false);

        Assert.Equal("Growatt MOD 5000TL3-X", inversor.Modelo);
        Assert.Equal("SN-999999", inversor.NumeroSerie);
        Assert.Equal(98.2, inversor.EficienciaConversion);
        Assert.False(inversor.VolcadoRedActivo);
    }
}
