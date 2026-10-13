using SolarUY.Domain.Entities;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Tests.Entities;

public class SensorDispositivoTests
{
    [Fact]
    public void Constructor_ConDatosValidos_NormalizaLaUrl()
    {
        var sensor = new SensorDispositivo("  ARM-001  ", "  1.4.2  ", true, "  http://localhost:5100/  ");

        Assert.NotEqual(Guid.Empty, sensor.Id);
        Assert.Equal("ARM-001", sensor.IdentificadorHardwareARM);
        Assert.Equal("1.4.2", sensor.VersionFirmware);
        Assert.True(sensor.Activo);
        Assert.Equal("http://localhost:5100", sensor.UrlBase);
    }

    [Fact]
    public void Constructor_ConUrlHttps_LaAcepta()
        => Assert.Equal("https://sensores.solaruy.uy",
            new SensorDispositivo("ARM-001", "1.0", true, "https://sensores.solaruy.uy").UrlBase);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("localhost:5100")]        // sin esquema: Uri lo lee como esquema "localhost"
    [InlineData("ftp://localhost:5100")]  // esquema que no es http ni https
    [InlineData("no es una url")]
    public void Constructor_ConUrlInvalida_LanzaReglaDeNegocio(string urlBase)
        => Assert.Throws<ReglaDeNegocioException>(() => new SensorDispositivo("ARM-001", "1.0", true, urlBase));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_SinIdentificadorDeHardware_LanzaReglaDeNegocio(string identificador)
        => Assert.Throws<ReglaDeNegocioException>(
            () => new SensorDispositivo(identificador, "1.0", true, "http://localhost:5100"));

    [Fact]
    public void ActivarYDesactivar_CambianElFlag()
    {
        var sensor = new SensorDispositivo("ARM-001", "1.0", true, "http://localhost:5100");

        sensor.Desactivar();
        Assert.False(sensor.Activo);

        sensor.Activar();
        Assert.True(sensor.Activo);
    }

    [Fact]
    public void Actualizar_CambiaLosDatos()
    {
        var sensor = new SensorDispositivo("ARM-001", "1.0", true, "http://localhost:5100");

        sensor.Actualizar("ARM-002", "2.1.0", false, "http://localhost:5200");

        Assert.Equal("ARM-002", sensor.IdentificadorHardwareARM);
        Assert.Equal("2.1.0", sensor.VersionFirmware);
        Assert.False(sensor.Activo);
        Assert.Equal("http://localhost:5200", sensor.UrlBase);
    }
}
