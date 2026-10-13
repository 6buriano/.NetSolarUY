using SolarUY.Domain.Entities;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Tests.Entities;

public class ParqueSolarTests
{
    [Fact]
    public void Constructor_ConDatosValidos_AsignaIdYNormalizaElTexto()
    {
        var parque = new ParqueSolar("  Parque Norte  ", "  Salto  ", 120.5, new DateTime(2026, 3, 10, 14, 30, 0));

        Assert.NotEqual(Guid.Empty, parque.Id);
        Assert.Equal("Parque Norte", parque.Nombre);
        Assert.Equal("Salto", parque.Ubicacion);
        Assert.Equal(120.5, parque.CapacidadInstaladaKW);
        Assert.Equal(new DateTime(2026, 3, 10), parque.FechaInstalacion);
        Assert.Empty(parque.Nodos);
        Assert.False(parque.TieneNodos());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_SinNombre_LanzaReglaDeNegocio(string nombre)
        => Assert.Throws<ReglaDeNegocioException>(() => new ParqueSolar(nombre, "Salto", 100d, DateTime.Today));

    [Fact]
    public void Constructor_SinUbicacion_LanzaReglaDeNegocio()
        => Assert.Throws<ReglaDeNegocioException>(() => new ParqueSolar("Parque", "   ", 100d, DateTime.Today));

    [Theory]
    [InlineData(0d)]
    [InlineData(-10d)]
    public void Constructor_ConCapacidadNoPositiva_LanzaReglaDeNegocio(double capacidad)
        => Assert.Throws<ReglaDeNegocioException>(() => new ParqueSolar("Parque", "Salto", capacidad, DateTime.Today));

    [Fact]
    public void Actualizar_CambiaLosDatosDelParque()
    {
        var parque = new ParqueSolar("Parque", "Salto", 100d, DateTime.Today);

        parque.Actualizar("Parque Sur", "Paysandú", 250d, new DateTime(2026, 5, 1));

        Assert.Equal("Parque Sur", parque.Nombre);
        Assert.Equal("Paysandú", parque.Ubicacion);
        Assert.Equal(250d, parque.CapacidadInstaladaKW);
        Assert.Equal(new DateTime(2026, 5, 1), parque.FechaInstalacion);
    }
}
