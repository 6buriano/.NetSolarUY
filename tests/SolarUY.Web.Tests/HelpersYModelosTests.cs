using System.ComponentModel.DataAnnotations;
using SolarUY.Domain.Enums;
using SolarUY.Web.Helpers;
using SolarUY.Web.Models;
using SolarUY.Web.Validation;

namespace SolarUY.Web.Tests;

/// <summary>
/// Pruebas de los helpers de las vistas (clases de las etiquetas y hora local), del validador de
/// direcciones y de las reglas de validación de los formularios.
/// </summary>
public class HelpersYModelosTests
{
    // ---------------------------------------------------------------- Etiquetas

    [Theory]
    [InlineData(EstadoSalud.OPERATIVO, "text-bg-success")]
    [InlineData(EstadoSalud.RENDIMIENTO_BAJO, "text-bg-warning")]
    [InlineData(EstadoSalud.FALLA_DETECTADA, "text-bg-danger")]
    [InlineData(EstadoSalud.DESCONECTADO, "text-bg-secondary")]
    public void EstadoSaludHelper_DevuelveLaClaseDeCadaEstado(EstadoSalud estado, string esperada)
    {
        Assert.Equal(esperada, EstadoSaludHelper.ClaseBadge(estado));
    }

    [Fact]
    public void EstadoSaludHelper_ConUnValorDesconocido_UsaLaClaseNeutra()
    {
        Assert.Equal("text-bg-secondary", EstadoSaludHelper.ClaseBadge((EstadoSalud)999));
    }

    [Theory]
    [InlineData(EstadoMantenimiento.PENDIENTE, "text-bg-secondary")]
    [InlineData(EstadoMantenimiento.EN_PROCESO, "text-bg-warning")]
    [InlineData(EstadoMantenimiento.COMPLETADO, "text-bg-success")]
    [InlineData(EstadoMantenimiento.CANCELADO, "text-bg-danger")]
    public void EstadoMantenimientoHelper_DevuelveLaClaseDeCadaEstado(EstadoMantenimiento estado, string esperada)
    {
        Assert.Equal(esperada, EstadoMantenimientoHelper.ClaseBadge(estado));
    }

    [Fact]
    public void EstadoMantenimientoHelper_ConUnValorDesconocido_UsaLaClaseNeutra()
    {
        Assert.Equal("text-bg-secondary", EstadoMantenimientoHelper.ClaseBadge((EstadoMantenimiento)999));
    }

    // ---------------------------------------------------------------- Hora local

    [Fact]
    public void HoraLocal_DesdeUtc_RestaTresHoras()
    {
        var utc = new DateTime(2026, 11, 12, 15, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 11, 12, 12, 30, 0), HoraLocal.DesdeUtc(utc));
    }

    [Fact]
    public void HoraLocal_DesdeUtc_CruzaElCambioDeDia()
    {
        var utc = new DateTime(2026, 11, 12, 1, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 11, 11, 22, 0, 0), HoraLocal.DesdeUtc(utc));
    }

    [Fact]
    public void HoraLocal_AParaUtc_SumaTresHoras()
    {
        var local = new DateTime(2026, 11, 12, 12, 30, 0, DateTimeKind.Unspecified);

        var utc = HoraLocal.AParaUtc(local);

        Assert.Equal(new DateTime(2026, 11, 12, 15, 30, 0), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    [Fact]
    public void HoraLocal_IdaYVuelta_DevuelveLaMismaHora()
    {
        var original = new DateTime(2026, 7, 1, 9, 15, 0, DateTimeKind.Utc);

        Assert.Equal(original, HoraLocal.AParaUtc(HoraLocal.DesdeUtc(original)));
    }

    [Fact]
    public void HoraLocal_ExponeLaEtiquetaYLaZona()
    {
        Assert.Equal("hora de Montevideo (UTC-3)", HoraLocal.Etiqueta);
        Assert.Equal("America/Montevideo", HoraLocal.NombreZona);
    }

    // ---------------------------------------------------------------- Dirección http

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://localhost:5100")]
    [InlineData("https://sensor.mi-parque.uy")]
    [InlineData("http://10.0.0.5:5100/")]
    public void DireccionHttp_AceptaVaciosYDireccionesCompletas(string? valor)
    {
        Assert.True(new DireccionHttpAttribute().IsValid(valor));
    }

    [Theory]
    [InlineData("sensor-a:5100")]
    [InlineData("ftp://sensor-a")]
    [InlineData("localhost")]
    public void DireccionHttp_RechazaLoQueNoEsUnaUrlHttp(string valor)
    {
        Assert.False(new DireccionHttpAttribute().IsValid(valor));
    }

    [Fact]
    public void DireccionHttp_ConUnValorQueNoEsTexto_NoLoValida()
    {
        Assert.True(new DireccionHttpAttribute().IsValid(42));
    }

    [Fact]
    public void DireccionHttp_TraeUnMensajeDeErrorEnEspanol()
    {
        var atributo = new DireccionHttpAttribute();

        Assert.Contains("http o https", atributo.ErrorMessage);
    }

    // ---------------------------------------------------------------- Formularios

    private static List<ValidationResult> Validar(object modelo)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, validateAllProperties: true);
        return resultados;
    }

    [Fact]
    public void PanelForm_PorDefecto_ProponePotenciaYEstadoOperativo()
    {
        var formulario = new PanelFormViewModel();

        Assert.Equal(550d, formulario.PotenciaNominalW, 6);
        Assert.Equal(EstadoSalud.OPERATIVO, formulario.Estado);
        Assert.True(formulario.Activo);
        Assert.Equal(DateTime.Today, formulario.FechaInstalacion);
        Assert.Empty(formulario.Nodos);
    }

    [Fact]
    public void PanelForm_ConCodigoConCaracteresInvalidos_LoRechaza()
    {
        var formulario = new PanelFormViewModel { CodigoPanel = "P 01!", Modelo = "M", Fabricante = "F" };

        var errores = Validar(formulario);

        Assert.Contains(errores, error => error.MemberNames.Contains(nameof(PanelFormViewModel.CodigoPanel)));
    }

    [Fact]
    public void PanelForm_ConDatosCompletos_EsValido()
    {
        var formulario = new PanelFormViewModel { CodigoPanel = "P01", Modelo = "JAM72S30", Fabricante = "JA Solar" };

        Assert.Empty(Validar(formulario));
    }

    [Fact]
    public void PanelForm_SinPotencia_LaRechaza()
    {
        var formulario = new PanelFormViewModel
        {
            CodigoPanel = "P01", Modelo = "M", Fabricante = "F", PotenciaNominalW = 0
        };

        Assert.Contains(Validar(formulario),
            error => error.MemberNames.Contains(nameof(PanelFormViewModel.PotenciaNominalW)));
    }

    [Fact]
    public void ParqueForm_SinCapacidad_LaRechaza()
    {
        var formulario = new ParqueFormViewModel { Nombre = "Parque Norte", Ubicacion = "Salto" };

        Assert.Contains(Validar(formulario),
            error => error.MemberNames.Contains(nameof(ParqueFormViewModel.CapacidadInstaladaKW)));
    }

    [Fact]
    public void ParqueForm_ConDatosCompletos_EsValidoYProponeLaFechaDeHoy()
    {
        var formulario = new ParqueFormViewModel { Nombre = "Parque Norte", Ubicacion = "Salto", CapacidadInstaladaKW = 10 };

        Assert.Empty(Validar(formulario));
        Assert.Equal(DateTime.Today, formulario.FechaInstalacion);
    }

    [Fact]
    public void NodoForm_ConIdentificadorConCaracteresInvalidos_LoRechaza()
    {
        var formulario = new NodoFormViewModel
        {
            Nombre = "Nodo", UbicacionDentroParque = "Sector", InversorModelo = "M", InversorNumeroSerie = "S",
            IdentificadorHardwareARM = "ARM 001!", SensorUrlBase = "http://localhost:5100"
        };

        Assert.Contains(Validar(formulario),
            error => error.MemberNames.Contains(nameof(NodoFormViewModel.IdentificadorHardwareARM)));
    }

    [Fact]
    public void NodoForm_ConDireccionInvalida_LaRechaza()
    {
        var formulario = new NodoFormViewModel
        {
            Nombre = "Nodo", UbicacionDentroParque = "Sector", InversorModelo = "M", InversorNumeroSerie = "S",
            IdentificadorHardwareARM = "ARM-001", SensorUrlBase = "sensor-a:5100"
        };

        Assert.Contains(Validar(formulario),
            error => error.MemberNames.Contains(nameof(NodoFormViewModel.SensorUrlBase)));
    }

    [Fact]
    public void NodoForm_PorDefecto_ProponeEficienciaYVolcadoActivo()
    {
        var formulario = new NodoFormViewModel();

        Assert.Equal(97.5, formulario.InversorEficienciaConversion, 6);
        Assert.True(formulario.InversorVolcadoRedActivo);
        Assert.True(formulario.SensorActivo);
        Assert.False(formulario.TieneSensor);
        Assert.Empty(formulario.Parques);
    }

    [Fact]
    public void NodoForm_TieneSensor_SoloConIdentificadorConTexto()
    {
        Assert.True(new NodoFormViewModel { IdentificadorHardwareARM = "ARM-001" }.TieneSensor);
        Assert.False(new NodoFormViewModel { IdentificadorHardwareARM = "   " }.TieneSensor);
    }
}
