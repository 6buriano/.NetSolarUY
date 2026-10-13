using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Tests.Entities;

public class PanelSolarTests
{
    private static readonly Guid NodoId = Guid.NewGuid();
    private static readonly DateTime FechaInstalacion = new(2026, 4, 2, 9, 15, 0);

    private static PanelSolar Crear(string codigoPanel = "P01", double potenciaNominalW = 550d,
        EstadoSalud estado = EstadoSalud.OPERATIVO, bool activo = true, Guid? nodoId = null)
        => new(codigoPanel, "JAM72S30-550/MR", "JA Solar", potenciaNominalW, estado,
            FechaInstalacion, nodoId ?? NodoId, activo);

    [Fact]
    public void Constructor_ConDatosValidos_AsignaIdYNormalizaElCodigo()
    {
        var panel = Crear("  P01  ");

        Assert.NotEqual(Guid.Empty, panel.Id);
        Assert.Equal("P01", panel.CodigoPanel);
        Assert.Equal(NodoId, panel.NodoId);
        Assert.Equal(550d, panel.PotenciaNominalW);
        Assert.Equal(EstadoSalud.OPERATIVO, panel.Estado);
        Assert.Equal(new DateTime(2026, 4, 2), panel.FechaInstalacion);
        Assert.True(panel.Activo);
        Assert.Empty(panel.Lecturas);
        Assert.Empty(panel.Mantenimientos);
    }

    [Fact]
    public void Constructor_ConActivoFalso_LoRespeta()
        => Assert.False(Crear(activo: false).Activo);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_SinCodigoDePanel_LanzaReglaDeNegocio(string codigoPanel)
        => Assert.Throws<ReglaDeNegocioException>(() => Crear(codigoPanel));

    [Theory]
    [InlineData("P 01")]                            // con espacio
    [InlineData("P#1")]                             // caracter no permitido
    [InlineData("P/1")]                             // rompería la URL del sensor
    [InlineData("codigo-demasiado-largo-123")]      // más de 20 caracteres
    public void Constructor_ConCodigoDePanelInvalido_LanzaReglaDeNegocio(string codigoPanel)
        => Assert.Throws<ReglaDeNegocioException>(() => Crear(codigoPanel));

    [Theory]
    [InlineData("P01")]
    [InlineData("panel-1")]
    [InlineData("A_B_9")]
    public void CodigoValido_AceptaLosCodigosQueUsaElSensor(string codigoPanel)
        => Assert.True(PanelSolar.CodigoValido(codigoPanel));

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void Constructor_ConPotenciaNoPositiva_LanzaReglaDeNegocio(double potencia)
        => Assert.Throws<ReglaDeNegocioException>(() => Crear(potenciaNominalW: potencia));

    [Fact]
    public void Constructor_ConEstadoFueraDelEnum_LanzaReglaDeNegocio()
        => Assert.Throws<ReglaDeNegocioException>(() => Crear(estado: (EstadoSalud)99));

    [Fact]
    public void Constructor_SinNodo_LanzaReglaDeNegocio()
        => Assert.Throws<ReglaDeNegocioException>(() => Crear(nodoId: Guid.Empty));

    [Fact]
    public void MarcarActividad_CambiaElFlag()
    {
        var panel = Crear();

        panel.MarcarActividad(false);
        Assert.False(panel.Activo);

        panel.MarcarActividad(true);
        Assert.True(panel.Activo);
    }

    [Fact]
    public void CambiarEstado_ActualizaElEstadoDelPanel()
    {
        var panel = Crear();

        panel.CambiarEstado(EstadoSalud.FALLA_DETECTADA);

        Assert.Equal(EstadoSalud.FALLA_DETECTADA, panel.Estado);
    }

    [Fact]
    public void Actualizar_CambiaLosDatosDelPanel()
    {
        var panel = Crear();
        var otroNodo = Guid.NewGuid();

        panel.Actualizar("P07", "TSM-585DE19", "Trina Solar", 585d, EstadoSalud.RENDIMIENTO_BAJO,
            new DateTime(2026, 5, 1), otroNodo, false);

        Assert.Equal("P07", panel.CodigoPanel);
        Assert.Equal("TSM-585DE19", panel.Modelo);
        Assert.Equal("Trina Solar", panel.Fabricante);
        Assert.Equal(585d, panel.PotenciaNominalW);
        Assert.Equal(EstadoSalud.RENDIMIENTO_BAJO, panel.Estado);
        Assert.Equal(new DateTime(2026, 5, 1), panel.FechaInstalacion);
        Assert.Equal(otroNodo, panel.NodoId);
        Assert.False(panel.Activo);
    }
}
