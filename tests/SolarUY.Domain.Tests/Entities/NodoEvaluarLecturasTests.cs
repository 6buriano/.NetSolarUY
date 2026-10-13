using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Tests.Entities;

/// <summary>
/// Regla de estado de la spec del simulador, sección 9: la caída de potencia se mide contra el
/// promedio de los otros paneles activos del mismo nodo, y la temperatura manda por encima de todo.
/// </summary>
public class NodoEvaluarLecturasTests
{
    private const double PotenciaNominal = 550d;

    private static readonly DateTime Mediodia = new(2026, 6, 1, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaInstalacion = new(2026, 1, 1);

    private static Nodo CrearNodo(int cantidadPaneles, double potenciaNominalW = PotenciaNominal)
    {
        var inversor = new Inversor("Huawei SUN2000-5KTL", Guid.NewGuid().ToString("N"), 97.5, true);
        var nodo = new Nodo("Nodo A", "Sector norte", Guid.NewGuid(), inversor);

        for (var i = 1; i <= cantidadPaneles; i++)
        {
            nodo.Paneles.Add(new PanelSolar($"P{i:D2}", "JAM72S30", "JA Solar", potenciaNominalW,
                EstadoSalud.OPERATIVO, FechaInstalacion, nodo.Id));
        }

        return nodo;
    }

    private static LecturaTelemetria Leer(PanelSolar panel, double potenciaW, double temperaturaCelsius = 30d)
        => new(panel.Id, Mediodia, 8d, 31d, potenciaW, temperaturaCelsius);

    private static EstadoSalud EstadoDe(IReadOnlyList<LecturaTelemetria> evaluadas, PanelSolar panel)
        => evaluadas.Single(lectura => lectura.PanelSolarId == panel.Id).EstadoDetectado;

    [Theory]
    [InlineData(0.00, EstadoSalud.OPERATIVO)]
    [InlineData(0.10, EstadoSalud.OPERATIVO)]
    [InlineData(0.15, EstadoSalud.OPERATIVO)]           // el umbral del 15 % es inclusivo
    [InlineData(0.151, EstadoSalud.RENDIMIENTO_BAJO)]
    [InlineData(0.30, EstadoSalud.RENDIMIENTO_BAJO)]
    [InlineData(0.40, EstadoSalud.RENDIMIENTO_BAJO)]    // el umbral del 40 % también
    [InlineData(0.401, EstadoSalud.FALLA_DETECTADA)]
    [InlineData(0.60, EstadoSalud.FALLA_DETECTADA)]
    public void EvaluarLecturas_ClasificaSegunLaCaidaRespectoDelPromedioDelNodo(double caida, EstadoSalud esperado)
    {
        var nodo = CrearNodo(3);
        var paneles = nodo.Paneles;

        var lecturas = new List<LecturaTelemetria>
        {
            Leer(paneles[0], PotenciaNominal),
            Leer(paneles[1], PotenciaNominal),
            Leer(paneles[2], PotenciaNominal * (1 - caida))
        };

        var evaluadas = nodo.EvaluarLecturas(lecturas);

        Assert.Equal(esperado, EstadoDe(evaluadas, paneles[2]));
        Assert.Equal(esperado, paneles[2].Estado);
    }

    [Fact]
    public void EvaluarLecturas_ConPanelesParejos_DejaTodoOperativo()
    {
        var nodo = CrearNodo(4);
        var lecturas = nodo.Paneles.Select(panel => Leer(panel, PotenciaNominal)).ToList();

        var evaluadas = nodo.EvaluarLecturas(lecturas);

        Assert.All(nodo.Paneles, panel => Assert.Equal(EstadoSalud.OPERATIVO, panel.Estado));
        Assert.All(evaluadas, lectura => Assert.Equal(EstadoSalud.OPERATIVO, lectura.EstadoDetectado));
    }

    [Theory]
    [InlineData(75d, EstadoSalud.OPERATIVO)]            // justo en el límite todavía no es falla
    [InlineData(75.1d, EstadoSalud.FALLA_DETECTADA)]
    [InlineData(90d, EstadoSalud.FALLA_DETECTADA)]
    public void EvaluarLecturas_LaTemperaturaMandaSobreLaPotencia(double temperatura, EstadoSalud esperado)
    {
        var nodo = CrearNodo(3);
        var paneles = nodo.Paneles;

        var lecturas = new List<LecturaTelemetria>
        {
            Leer(paneles[0], PotenciaNominal),
            Leer(paneles[1], PotenciaNominal),
            Leer(paneles[2], PotenciaNominal, temperatura)
        };

        var evaluadas = nodo.EvaluarLecturas(lecturas);

        Assert.Equal(esperado, EstadoDe(evaluadas, paneles[2]));
    }

    [Fact]
    public void EvaluarLecturas_ConSobretemperaturaDeNoche_IgualDetectaLaFalla()
    {
        var nodo = CrearNodo(3);
        var paneles = nodo.Paneles;

        // De noche no hay generación, así que la caída de potencia no se evalúa; la temperatura sí.
        var lecturas = new List<LecturaTelemetria>
        {
            Leer(paneles[0], 0d, 18d),
            Leer(paneles[1], 0d, 18d),
            Leer(paneles[2], 0d, 85d)
        };

        var evaluadas = nodo.EvaluarLecturas(lecturas);

        Assert.Equal(EstadoSalud.FALLA_DETECTADA, EstadoDe(evaluadas, paneles[2]));
        Assert.Equal(EstadoSalud.OPERATIVO, EstadoDe(evaluadas, paneles[0]));
    }

    [Fact]
    public void EvaluarLecturas_ConPocaLuz_NoEvaluaLaCaidaParaNoDarFalsosPositivos()
    {
        var nodo = CrearNodo(3);
        var paneles = nodo.Paneles;

        // Todos por debajo del 5 % de la nominal: el panel 3 rinde la décima parte, pero no se evalúa.
        var lecturas = new List<LecturaTelemetria>
        {
            Leer(paneles[0], 10d),
            Leer(paneles[1], 10d),
            Leer(paneles[2], 1d)
        };

        var evaluadas = nodo.EvaluarLecturas(lecturas);

        Assert.Equal(EstadoSalud.OPERATIVO, EstadoDe(evaluadas, paneles[2]));
    }

    [Fact]
    public void EvaluarLecturas_ConUnSoloPanelActivoConLectura_NoTieneReferenciaYNoEvaluaLaCaida()
    {
        var nodo = CrearNodo(2);
        var paneles = nodo.Paneles;
        paneles[1].MarcarActividad(false);

        var lecturas = new List<LecturaTelemetria>
        {
            Leer(paneles[0], 100d),
            Leer(paneles[1], 0d)
        };

        var evaluadas = nodo.EvaluarLecturas(lecturas);

        Assert.Equal(EstadoSalud.OPERATIVO, EstadoDe(evaluadas, paneles[0]));
        Assert.Equal(EstadoSalud.DESCONECTADO, EstadoDe(evaluadas, paneles[1]));
    }

    [Fact]
    public void EvaluarLecturas_ConPanelInactivo_LoMarcaDesconectado()
    {
        var nodo = CrearNodo(3);
        var paneles = nodo.Paneles;
        paneles[2].MarcarActividad(false);

        var lecturas = new List<LecturaTelemetria>
        {
            Leer(paneles[0], PotenciaNominal),
            Leer(paneles[1], PotenciaNominal),
            Leer(paneles[2], 0d)
        };

        var evaluadas = nodo.EvaluarLecturas(lecturas);

        Assert.Equal(EstadoSalud.DESCONECTADO, EstadoDe(evaluadas, paneles[2]));
        Assert.Equal(EstadoSalud.DESCONECTADO, paneles[2].Estado);
    }

    [Fact]
    public void EvaluarLecturas_ConPanelPorEncimaDelPromedio_EsOperativo()
    {
        var nodo = CrearNodo(2);
        var paneles = nodo.Paneles;

        var lecturas = new List<LecturaTelemetria>
        {
            Leer(paneles[0], PotenciaNominal),
            Leer(paneles[1], 200d)                       // cae 63 % respecto de su hermano
        };

        var evaluadas = nodo.EvaluarLecturas(lecturas);

        // El panel bueno queda por encima del promedio (caída negativa) y el otro en falla.
        Assert.Equal(EstadoSalud.OPERATIVO, EstadoDe(evaluadas, paneles[0]));
        Assert.Equal(EstadoSalud.FALLA_DETECTADA, EstadoDe(evaluadas, paneles[1]));
    }

    [Fact]
    public void EvaluarLecturas_NormalizaPanelesDeDistintaPotenciaNominal()
    {
        var nodo = CrearNodo(2);
        nodo.Paneles.Add(new PanelSolar("P03", "TSM-585DE19", "Trina Solar", 585d,
            EstadoSalud.OPERATIVO, FechaInstalacion, nodo.Id));

        // Cada panel genera su propia potencia nominal: el rendimiento relativo es 1 en los tres,
        // aunque los watts crudos sean distintos.
        var lecturas = new List<LecturaTelemetria>
        {
            Leer(nodo.Paneles[0], 550d),
            Leer(nodo.Paneles[1], 550d),
            Leer(nodo.Paneles[2], 585d)
        };

        var evaluadas = nodo.EvaluarLecturas(lecturas);

        Assert.All(nodo.Paneles, panel => Assert.Equal(EstadoSalud.OPERATIVO, panel.Estado));
        Assert.All(evaluadas, lectura => Assert.Equal(EstadoSalud.OPERATIVO, lectura.EstadoDetectado));
    }

    [Fact]
    public void EvaluarLecturas_ConUnPanelSinLectura_LeConservaElEstado()
    {
        var nodo = CrearNodo(2);
        var paneles = nodo.Paneles;
        paneles[1].CambiarEstado(EstadoSalud.RENDIMIENTO_BAJO);

        nodo.EvaluarLecturas(new List<LecturaTelemetria> { Leer(paneles[0], PotenciaNominal) });

        Assert.Equal(EstadoSalud.RENDIMIENTO_BAJO, paneles[1].Estado);
    }

    [Fact]
    public void EvaluarLecturas_ConLecturaDeOtroNodo_LanzaReglaDeNegocio()
    {
        var nodo = CrearNodo(2);
        var lecturaAjena = new LecturaTelemetria(Guid.NewGuid(), Mediodia, 8d, 31d, 100d, 30d);

        Assert.Throws<ReglaDeNegocioException>(() => nodo.EvaluarLecturas(new List<LecturaTelemetria> { lecturaAjena }));
    }

    [Fact]
    public void EvaluarLecturas_SinLecturas_NoRompe()
    {
        var nodo = CrearNodo(2);

        var evaluadas = nodo.EvaluarLecturas([]);

        Assert.Empty(evaluadas);
        Assert.All(nodo.Paneles, panel => Assert.Equal(EstadoSalud.OPERATIVO, panel.Estado));
    }
}
