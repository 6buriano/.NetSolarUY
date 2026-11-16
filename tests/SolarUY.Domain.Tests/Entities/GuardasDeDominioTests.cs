using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Tests.Entities;

/// <summary>
/// Guardas de las entidades que las otras pruebas no recorren: datos obligatorios, rangos y
/// el constructor sin parámetros que usa EF Core para reconstruir las entidades desde la base.
/// </summary>
public class GuardasDeDominioTests
{
    private static Inversor CrearInversor() => new("Huawei SUN2000-5KTL", "SN-000123", 97.5, true);

    // ---------------------------------------------------------------- Inversor

    [Theory]
    [InlineData("", "SN-1", 97.5)]
    [InlineData("   ", "SN-1", 97.5)]
    [InlineData("Huawei", "", 97.5)]
    [InlineData("Huawei", "   ", 97.5)]
    [InlineData("Huawei", "SN-1", 0d)]
    [InlineData("Huawei", "SN-1", -1d)]
    [InlineData("Huawei", "SN-1", 100.01)]
    public void Inversor_ConDatosInvalidos_LanzaReglaDeNegocio(string modelo, string numeroSerie, double eficiencia)
    {
        Assert.Throws<ReglaDeNegocioException>(() => new Inversor(modelo, numeroSerie, eficiencia, true));
    }

    [Fact]
    public void Inversor_ConEficienciaEnElLimiteSuperior_LaAcepta()
    {
        var inversor = new Inversor("Huawei", "SN-1", 100d, false);

        Assert.Equal(100d, inversor.EficienciaConversion, 6);
        Assert.False(inversor.VolcadoRedActivo);
    }

    [Fact]
    public void Inversor_Actualizar_ConDatosInvalidos_NoCambiaNada()
    {
        var inversor = CrearInversor();

        Assert.Throws<ReglaDeNegocioException>(() => inversor.Actualizar("Otro", "SN-2", 0d, false));

        Assert.Equal("Huawei SUN2000-5KTL", inversor.Modelo);
        Assert.Equal("SN-000123", inversor.NumeroSerie);
    }

    [Fact]
    public void Inversor_RegistrarSensorNulo_LanzaReglaDeNegocio()
    {
        var inversor = CrearInversor();

        var error = Assert.Throws<ReglaDeNegocioException>(() => inversor.RegistrarSensor(null!));

        Assert.Contains("no puede ser nulo", error.Message);
        Assert.Null(inversor.Sensor);
    }

    [Fact]
    public void Inversor_QuitarSensor_DejaAlInversorSinSensor()
    {
        var inversor = CrearInversor();
        inversor.RegistrarSensor(new SensorDispositivo("ARM-001", "1.0", true, "http://localhost:5100"));

        inversor.QuitarSensor();

        Assert.Null(inversor.Sensor);
    }

    // ---------------------------------------------------------------- Nodo

    [Fact]
    public void Nodo_SinUbicacion_LanzaReglaDeNegocio()
    {
        Assert.Throws<ReglaDeNegocioException>(() => new Nodo("Nodo A", "  ", Guid.NewGuid(), CrearInversor()));
    }

    [Fact]
    public void Nodo_Actualizar_ConParqueVacio_LanzaReglaDeNegocio()
    {
        var nodo = new Nodo("Nodo A", "Sector 1", Guid.NewGuid(), CrearInversor());

        Assert.Throws<ReglaDeNegocioException>(() => nodo.Actualizar("Nodo B", "Sector 2", Guid.Empty));
        Assert.Equal("Nodo A", nodo.Nombre);
    }

    [Fact]
    public void Nodo_EvaluarLecturas_ConListaNula_LanzaArgumentNull()
    {
        var nodo = new Nodo("Nodo A", "Sector 1", Guid.NewGuid(), CrearInversor());

        Assert.Throws<ArgumentNullException>(() => nodo.EvaluarLecturas(null!));
    }

    [Fact]
    public void Nodo_EvaluarLecturas_ConLecturaDeUnPanelAjeno_LanzaReglaDeNegocio()
    {
        var nodo = new Nodo("Nodo A", "Sector 1", Guid.NewGuid(), CrearInversor());
        var ajena = new LecturaTelemetria(Guid.NewGuid(), DateTime.UtcNow, 8d, 40d, 320d, 30d);

        var error = Assert.Throws<ReglaDeNegocioException>(() => nodo.EvaluarLecturas([ajena]));

        Assert.Contains("no corresponden a paneles de este nodo", error.Message);
    }

    // ---------------------------------------------------------------- PanelSolar

    [Theory]
    [InlineData("", "JA Solar")]
    [InlineData("   ", "JA Solar")]
    [InlineData("JAM72S30", "")]
    [InlineData("JAM72S30", "   ")]
    public void PanelSolar_SinModeloOFabricante_LanzaReglaDeNegocio(string modelo, string fabricante)
    {
        Assert.Throws<ReglaDeNegocioException>(() => new PanelSolar("P01", modelo, fabricante, 550d,
            EstadoSalud.OPERATIVO, new DateTime(2026, 1, 1), Guid.NewGuid()));
    }

    [Fact]
    public void PanelSolar_Actualizar_ConPotenciaCero_LanzaReglaDeNegocio()
    {
        var panel = new PanelSolar("P01", "JAM72S30", "JA Solar", 550d, EstadoSalud.OPERATIVO,
            new DateTime(2026, 1, 1), Guid.NewGuid());

        Assert.Throws<ReglaDeNegocioException>(() => panel.Actualizar("P01", "JAM72S30", "JA Solar", 0d,
            EstadoSalud.OPERATIVO, new DateTime(2026, 1, 1), panel.NodoId, true));
        Assert.Equal(550d, panel.PotenciaNominalW, 6);
    }

    // ---------------------------------------------------------------- LecturaTelemetria

    [Fact]
    public void Lectura_SinPanel_LanzaReglaDeNegocio()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            new LecturaTelemetria(Guid.Empty, DateTime.UtcNow, 8d, 40d, 320d, 30d));
    }

    [Theory]
    [InlineData(-0.1, 40d, 320d, 30d)]
    [InlineData(8d, -0.1, 320d, 30d)]
    [InlineData(8d, 40d, -0.1, 30d)]
    [InlineData(8d, 40d, 320d, -50.1)]
    [InlineData(8d, 40d, 320d, 150.1)]
    public void Lectura_ConValoresFueraDeRango_LanzaReglaDeNegocio(double corriente, double voltaje,
        double potencia, double temperatura)
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            new LecturaTelemetria(Guid.NewGuid(), DateTime.UtcNow, corriente, voltaje, potencia, temperatura));
    }

    [Fact]
    public void Lectura_ConTemperaturasEnLosLimites_LasAcepta()
    {
        var fria = new LecturaTelemetria(Guid.NewGuid(), DateTime.UtcNow, 0d, 0d, 0d, -50d);
        var caliente = new LecturaTelemetria(Guid.NewGuid(), DateTime.UtcNow, 0d, 0d, 0d, 150d);

        Assert.Equal(-50d, fria.TemperaturaCelsius, 6);
        Assert.Equal(150d, caliente.TemperaturaCelsius, 6);
        Assert.Equal(EstadoSalud.OPERATIVO, fria.EstadoDetectado);
    }

    // ---------------------------------------------------------------- ParqueSolar y sensor

    [Fact]
    public void Parque_TieneNodos_SoloConNodosCargados()
    {
        var parque = new ParqueSolar("Parque Norte", "Salto", 12.5, new DateTime(2026, 1, 1));
        Assert.False(parque.TieneNodos());

        parque.Nodos.Add(new Nodo("Nodo A", "Sector 1", parque.Id, CrearInversor()));

        Assert.True(parque.TieneNodos());
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("sensor-a:5100", false)]
    [InlineData("ftp://sensor-a", false)]
    [InlineData("http://sensor-a:5100/", true)]
    [InlineData("  https://sensor.mi-parque.uy  ", true)]
    public void Sensor_DireccionValida_SoloAceptaHttpYHttpsAbsolutas(string? url, bool esperado)
    {
        Assert.Equal(esperado, SensorDispositivo.DireccionValida(url));
    }

    // ---------------------------------------------------------------- Excepción de negocio

    [Fact]
    public void ReglaDeNegocioException_ConservaElMensajeYLaCausa()
    {
        var causa = new InvalidOperationException("origen");

        var simple = new ReglaDeNegocioException("regla");
        var conCausa = new ReglaDeNegocioException("regla", causa);

        Assert.Equal("regla", simple.Message);
        Assert.Null(simple.InnerException);
        Assert.Same(causa, conCausa.InnerException);
    }

    // ---------------------------------------------------------------- Constructores de EF Core

    [Theory]
    [InlineData(typeof(Inversor))]
    [InlineData(typeof(Nodo))]
    [InlineData(typeof(PanelSolar))]
    [InlineData(typeof(ParqueSolar))]
    [InlineData(typeof(SensorDispositivo))]
    [InlineData(typeof(LecturaTelemetria))]
    [InlineData(typeof(TareaMantenimiento))]
    public void ConstructorSinParametros_PermiteReconstruirLaEntidadVacia(Type tipo)
    {
        var entidad = Activator.CreateInstance(tipo, nonPublic: true);

        Assert.NotNull(entidad);
        Assert.Equal(tipo, entidad.GetType());
    }
}
