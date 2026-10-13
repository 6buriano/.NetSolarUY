using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Tests.Entities;

/// <summary>
/// Regla de negocio 1: un nodo debe tener exactamente un inversor asignado.
/// </summary>
public class NodoTests
{
    private static Inversor CrearInversor() => new("Huawei SUN2000-5KTL", "SN-000123", 97.5, true);

    [Fact]
    public void Constructor_ConInversor_DejaElNodoAsignadoEnElInversor()
    {
        var inversor = CrearInversor();
        var parqueSolarId = Guid.NewGuid();

        var nodo = new Nodo("Nodo A", "Sector norte, fila 1", parqueSolarId, inversor);

        Assert.NotEqual(Guid.Empty, nodo.Id);
        Assert.Equal(parqueSolarId, nodo.ParqueSolarId);
        Assert.Same(inversor, nodo.Inversor);
        Assert.Equal(nodo.Id, inversor.NodoId);
    }

    [Fact]
    public void Constructor_SinInversor_LanzaReglaDeNegocio()
        => Assert.Throws<ReglaDeNegocioException>(
            () => new Nodo("Nodo A", "Sector norte", Guid.NewGuid(), null!));

    [Fact]
    public void Constructor_SinParque_LanzaReglaDeNegocio()
        => Assert.Throws<ReglaDeNegocioException>(
            () => new Nodo("Nodo A", "Sector norte", Guid.Empty, CrearInversor()));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_SinNombre_LanzaReglaDeNegocio(string nombre)
        => Assert.Throws<ReglaDeNegocioException>(
            () => new Nodo(nombre, "Sector norte", Guid.NewGuid(), CrearInversor()));

    [Fact]
    public void TienePaneles_SinPaneles_DevuelveFalse()
    {
        var nodo = new Nodo("Nodo A", "Sector norte", Guid.NewGuid(), CrearInversor());

        Assert.False(nodo.TienePaneles());
    }

    [Fact]
    public void TienePaneles_ConUnPanel_DevuelveTrue()
    {
        var nodo = new Nodo("Nodo A", "Sector norte", Guid.NewGuid(), CrearInversor());

        nodo.Paneles.Add(new PanelSolar("P01", "JAM72S30-550/MR", "JA Solar", 550d, EstadoSalud.OPERATIVO,
            DateTime.Today, nodo.Id));

        Assert.True(nodo.TienePaneles());
        Assert.Single(nodo.Paneles);
    }

    [Fact]
    public void BuscarPanelPorCodigo_NoDistingueMayusculas()
    {
        var nodo = new Nodo("Nodo A", "Sector norte", Guid.NewGuid(), CrearInversor());
        var panel = new PanelSolar("P01", "JAM72S30-550/MR", "JA Solar", 550d, EstadoSalud.OPERATIVO,
            DateTime.Today, nodo.Id);
        nodo.Paneles.Add(panel);

        // El simulador compara los códigos sin distinguir mayúsculas, así que el dominio hace lo mismo.
        Assert.Same(panel, nodo.BuscarPanelPorCodigo("p01"));
        Assert.Same(panel, nodo.BuscarPanelPorCodigo("P01"));
        Assert.Null(nodo.BuscarPanelPorCodigo("P99"));
    }

    [Fact]
    public void Actualizar_ConDatosValidos_CambiaNombreYParque()
    {
        var nodo = new Nodo("Nodo A", "Sector norte", Guid.NewGuid(), CrearInversor());
        var nuevoParqueId = Guid.NewGuid();

        nodo.Actualizar("Nodo B", "Sector sur", nuevoParqueId);

        Assert.Equal("Nodo B", nodo.Nombre);
        Assert.Equal("Sector sur", nodo.UbicacionDentroParque);
        Assert.Equal(nuevoParqueId, nodo.ParqueSolarId);
    }
}
