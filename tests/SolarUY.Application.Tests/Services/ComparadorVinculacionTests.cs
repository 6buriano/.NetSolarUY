using SolarUY.Application.Diagnostics;
using SolarUY.Application.Sensors;
using SolarUY.Domain.Entities;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="ComparadorVinculacion"/>: cruza los nodos cargados en el sistema con el inventario
/// del simulador y arma el informe. Cada prueba arma una sola discrepancia para ver qué hallazgo produce.
/// </summary>
public class ComparadorVinculacionTests
{
    private static InventarioSensorDto SensorSimulado(string hardware, params (string Codigo, double Potencia)[] paneles)
        => new(hardware, true,
            paneles.Select(panel => new InventarioPanelDto(panel.Codigo, panel.Potencia, true, null, false)).ToList());

    /// <summary>Un nodo con sensor y con los paneles indicados, todos de 550 W salvo que se pida otra cosa.</summary>
    private static Nodo NodoConPaneles(string hardware, bool sensorActivo = true, double potenciaW = 550d,
        params string[] codigos)
    {
        var nodo = FixtureAbm.NuevoNodo(hardware: hardware, sensorActivo: sensorActivo);

        foreach (var codigo in codigos)
        {
            nodo.Paneles.Add(FixtureAbm.NuevoPanel(nodo.Id, codigo, potenciaW));
        }

        return nodo;
    }

    [Fact]
    public void Comparar_SiTodoCoincide_NoHayHallazgos()
    {
        var nodo = NodoConPaneles("ARM-001", true, 550d, "P01", "P02");
        var inventario = new[] { SensorSimulado("ARM-001", ("P01", 550d), ("P02", 550d)) };

        var informe = ComparadorVinculacion.Comparar([nodo], inventario);

        Assert.True(informe.SimuladorConsultado);
        Assert.Null(informe.ErrorConsulta);
        Assert.True(informe.TodoVinculado);
        Assert.Empty(informe.Hallazgos);
        Assert.Equal(0, informe.Errores);
        Assert.Equal(0, informe.Advertencias);
        Assert.Equal(1, informe.NodosRevisados);
        Assert.Equal(2, informe.PanelesRevisados);
        Assert.Equal(1, informe.SensoresEnElSimulador);
        Assert.Empty(informe.SensoresLibres);
    }

    [Fact]
    public void Comparar_SiNoHayNodosNiInventario_DevuelveUnInformeVacioYVinculado()
    {
        var informe = ComparadorVinculacion.Comparar([], []);

        Assert.True(informe.TodoVinculado);
        Assert.Equal(0, informe.NodosRevisados);
        Assert.Equal(0, informe.SensoresEnElSimulador);
    }

    [Fact]
    public void Comparar_SiElNodoNoTieneSensor_InformaUnError()
    {
        var nodo = FixtureAbm.NuevoNodo(nombre: "Nodo Sin Sensor");
        nodo.Paneles.Add(FixtureAbm.NuevoPanel(nodo.Id));

        var informe = ComparadorVinculacion.Comparar([nodo], []);

        var hallazgo = Assert.Single(informe.Hallazgos);
        Assert.Equal(SeveridadHallazgo.Error, hallazgo.Severidad);
        Assert.Equal("Nodo Sin Sensor", hallazgo.Nodo);
        Assert.Null(hallazgo.Sensor);
        Assert.False(informe.TodoVinculado);
        Assert.Equal(1, informe.Errores);
        // Un nodo sin sensor no suma sus paneles a los revisados.
        Assert.Equal(0, informe.PanelesRevisados);
    }

    [Fact]
    public void Comparar_SiElSensorEstaDesactivado_InformaUnaAdvertencia()
    {
        var nodo = NodoConPaneles("ARM-001", sensorActivo: false, codigos: "P01");
        var inventario = new[] { SensorSimulado("ARM-001", ("P01", 550d)) };

        var informe = ComparadorVinculacion.Comparar([nodo], inventario);

        var hallazgo = Assert.Single(informe.Hallazgos);
        Assert.Equal(SeveridadHallazgo.Advertencia, hallazgo.Severidad);
        Assert.Equal("ARM-001", hallazgo.Sensor);
        Assert.Contains("desactivado", hallazgo.Detalle);
        Assert.Equal(1, informe.Advertencias);
        Assert.Equal(0, informe.Errores);
    }

    [Fact]
    public void Comparar_SiElSimuladorNoEmulaElSensor_InformaUnErrorYListaLosQueExisten()
    {
        var nodo = NodoConPaneles("ARM-999", codigos: "P01");
        var inventario = new[] { SensorSimulado("ARM-002"), SensorSimulado("ARM-001") };

        var informe = ComparadorVinculacion.Comparar([nodo], inventario);

        var hallazgo = Assert.Single(informe.Hallazgos);
        Assert.Equal(SeveridadHallazgo.Error, hallazgo.Severidad);
        Assert.Contains("ARM-999", hallazgo.Detalle);
        // Los sensores que existen se listan ordenados.
        Assert.Contains("ARM-001, ARM-002", hallazgo.Detalle);
    }

    [Fact]
    public void Comparar_SiElSimuladorNoTieneNingunSensor_LoDiceEnElDetalle()
    {
        var nodo = NodoConPaneles("ARM-999", codigos: "P01");

        var informe = ComparadorVinculacion.Comparar([nodo], []);

        var hallazgo = Assert.Single(informe.Hallazgos);
        Assert.Contains("ninguno", hallazgo.Detalle);
    }

    [Fact]
    public void Comparar_IgnoraMayusculasAlBuscarElSensorYElPanel()
    {
        var nodo = NodoConPaneles("arm-001", codigos: "p01");
        var inventario = new[] { SensorSimulado("ARM-001", ("P01", 550d)) };

        var informe = ComparadorVinculacion.Comparar([nodo], inventario);

        Assert.True(informe.TodoVinculado);
    }

    [Fact]
    public void Comparar_SiUnPanelNoExisteEnElSensor_InformaUnError()
    {
        var nodo = NodoConPaneles("ARM-001", codigos: ["P01", "P02"]);
        var inventario = new[] { SensorSimulado("ARM-001", ("P01", 550d)) };

        var informe = ComparadorVinculacion.Comparar([nodo], inventario);

        var hallazgo = Assert.Single(informe.Hallazgos);
        Assert.Equal(SeveridadHallazgo.Error, hallazgo.Severidad);
        Assert.Contains("'P02'", hallazgo.Detalle);
        Assert.Contains("P01", hallazgo.Detalle);
    }

    [Fact]
    public void Comparar_SiLaPotenciaDelPanelNoCoincide_InformaUnaAdvertencia()
    {
        var nodo = NodoConPaneles("ARM-001", potenciaW: 550d, codigos: "P01");
        var inventario = new[] { SensorSimulado("ARM-001", ("P01", 400d)) };

        var informe = ComparadorVinculacion.Comparar([nodo], inventario);

        var hallazgo = Assert.Single(informe.Hallazgos);
        Assert.Equal(SeveridadHallazgo.Advertencia, hallazgo.Severidad);
        Assert.Contains("'P01'", hallazgo.Detalle);
    }

    [Fact]
    public void Comparar_SiLaDiferenciaDePotenciaEsMenorAUnCentesimo_NoLaTomaPorDistinta()
    {
        var nodo = NodoConPaneles("ARM-001", potenciaW: 550d, codigos: "P01");
        var inventario = new[] { SensorSimulado("ARM-001", ("P01", 550.005d)) };

        var informe = ComparadorVinculacion.Comparar([nodo], inventario);

        Assert.True(informe.TodoVinculado);
    }

    [Fact]
    public void Comparar_SiElSensorInformaUnPanelQueElSistemaNoTiene_InformaUnaAdvertencia()
    {
        var nodo = NodoConPaneles("ARM-001", codigos: "P01");
        var inventario = new[] { SensorSimulado("ARM-001", ("P01", 550d), ("P09", 550d)) };

        var informe = ComparadorVinculacion.Comparar([nodo], inventario);

        var hallazgo = Assert.Single(informe.Hallazgos);
        Assert.Equal(SeveridadHallazgo.Advertencia, hallazgo.Severidad);
        Assert.Contains("'P09'", hallazgo.Detalle);
        Assert.Contains("se descartan", hallazgo.Detalle);
    }

    [Fact]
    public void Comparar_ListaOrdenadosLosSensoresDelSimuladorQueNingunNodoUsa()
    {
        var nodo = NodoConPaneles("ARM-001", codigos: "P01");
        var inventario = new[]
        {
            SensorSimulado("ARM-009"),
            SensorSimulado("ARM-001", ("P01", 550d)),
            SensorSimulado("ARM-003")
        };

        var informe = ComparadorVinculacion.Comparar([nodo], inventario);

        Assert.Equal("ARM-003,ARM-009", string.Join(",", informe.SensoresLibres));
        Assert.Equal(3, informe.SensoresEnElSimulador);
    }

    [Fact]
    public void Comparar_AcumulaLosHallazgosDeVariosNodos()
    {
        var sinSensor = FixtureAbm.NuevoNodo(nombre: "Sin Sensor", numeroSerie: "SN-A");
        var desactivado = NodoConPaneles("ARM-001", sensorActivo: false, codigos: "P01");
        var inventario = new[] { SensorSimulado("ARM-001", ("P01", 550d)) };

        var informe = ComparadorVinculacion.Comparar([sinSensor, desactivado], inventario);

        Assert.Equal(2, informe.Hallazgos.Count);
        Assert.Equal(1, informe.Errores);
        Assert.Equal(1, informe.Advertencias);
        Assert.Equal(2, informe.NodosRevisados);
    }

    [Fact]
    public void Comparar_SiLaListaDeNodosEsNula_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => { ComparadorVinculacion.Comparar(null!, []); });
    }

    [Fact]
    public void Comparar_SiElInventarioEsNulo_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => { ComparadorVinculacion.Comparar([], null!); });
    }

    [Fact]
    public void SinSimulador_ArmaUnInformeQueNoSeConsulto()
    {
        var informe = InformeVinculacion.SinSimulador("no responde", 3);

        Assert.False(informe.SimuladorConsultado);
        Assert.Equal("no responde", informe.ErrorConsulta);
        Assert.Equal(3, informe.NodosRevisados);
        Assert.False(informe.TodoVinculado);
        Assert.Empty(informe.Hallazgos);
    }
}
