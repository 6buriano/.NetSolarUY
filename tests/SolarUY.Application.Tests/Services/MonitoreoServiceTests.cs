using SolarUY.Application.Services;
using SolarUY.Domain.Enums;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="MonitoreoService.ArmarInstantanea"/>, que es pura: recibe los parques y las
/// lecturas de la ventana y no toca la base.
/// </summary>
public class MonitoreoServiceTests
{
    private static readonly DateTime Ahora = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ArmarInstantanea_ConVariasLecturasDelMismoPanel_TomaLaMasReciente()
    {
        var parque = FixtureMonitoreo.Parque();
        var nodo = FixtureMonitoreo.Nodo(parque);
        var panel = FixtureMonitoreo.Panel(nodo, "P01");

        var instantanea = MonitoreoService.ArmarInstantanea([parque],
        [
            FixtureMonitoreo.Lectura(panel, Ahora.AddSeconds(-10), 100),
            FixtureMonitoreo.Lectura(panel, Ahora.AddSeconds(-5), 140)
        ], Ahora);

        var panelDto = instantanea.Parques[0].Nodos[0].Paneles[0];
        Assert.Equal(140, panelDto.PotenciaActualW);
        Assert.Equal(Ahora.AddSeconds(-5), panelDto.UltimaLecturaUtc);
    }

    [Fact]
    public void ArmarInstantanea_PanelSinLectura_QuedaSinDatosYNoSuma()
    {
        var parque = FixtureMonitoreo.Parque();
        var nodo = FixtureMonitoreo.Nodo(parque);
        var conLectura = FixtureMonitoreo.Panel(nodo, "P01");
        FixtureMonitoreo.Panel(nodo, "P02");

        var instantanea = MonitoreoService.ArmarInstantanea([parque],
            [FixtureMonitoreo.Lectura(conLectura, Ahora, 150)], Ahora);

        var panelSinLectura = instantanea.Parques[0].Nodos[0].Paneles.Single(panel => panel.CodigoPanel == "P02");
        Assert.Null(panelSinLectura.PotenciaActualW);
        Assert.Null(panelSinLectura.TemperaturaCelsius);
        Assert.Null(panelSinLectura.UltimaLecturaUtc);

        // El nodo suma solo lo que tiene lectura.
        Assert.Equal(150, instantanea.Parques[0].Nodos[0].PotenciaActualW);
        Assert.Equal(150, instantanea.Parques[0].PotenciaActualW);
    }

    [Fact]
    public void ArmarInstantanea_PanelInactivo_NoSumaAunqueTengaLectura()
    {
        var parque = FixtureMonitoreo.Parque();
        var nodo = FixtureMonitoreo.Nodo(parque);
        var activo = FixtureMonitoreo.Panel(nodo, "P01");
        var inactivo = FixtureMonitoreo.Panel(nodo, "P02", activo: false);

        var instantanea = MonitoreoService.ArmarInstantanea([parque],
        [
            FixtureMonitoreo.Lectura(activo, Ahora, 100),
            FixtureMonitoreo.Lectura(inactivo, Ahora, 250)
        ], Ahora);

        // La lectura se muestra, pero el panel inactivo no aporta a la potencia del nodo.
        var panelInactivo = instantanea.Parques[0].Nodos[0].Paneles.Single(panel => panel.CodigoPanel == "P02");
        Assert.Equal(250, panelInactivo.PotenciaActualW);
        Assert.False(panelInactivo.Activo);
        Assert.Equal(100, instantanea.Parques[0].Nodos[0].PotenciaActualW);
        Assert.Equal(100, instantanea.Parques[0].PotenciaActualW);
    }

    [Fact]
    public void ArmarInstantanea_SumaLaPotenciaDeLosNodosYDelParque()
    {
        var parque = FixtureMonitoreo.Parque();
        var nodoA = FixtureMonitoreo.Nodo(parque, "Nodo A");
        var nodoB = FixtureMonitoreo.Nodo(parque, "Nodo B");
        var panelA1 = FixtureMonitoreo.Panel(nodoA, "P01");
        var panelA2 = FixtureMonitoreo.Panel(nodoA, "P02");
        var panelB1 = FixtureMonitoreo.Panel(nodoB, "P01");

        var instantanea = MonitoreoService.ArmarInstantanea([parque],
        [
            FixtureMonitoreo.Lectura(panelA1, Ahora, 100),
            FixtureMonitoreo.Lectura(panelA2, Ahora, 50),
            FixtureMonitoreo.Lectura(panelB1, Ahora, 200)
        ], Ahora);

        var nodoDtoA = instantanea.Parques[0].Nodos.Single(nodo => nodo.Nombre == "Nodo A");
        var nodoDtoB = instantanea.Parques[0].Nodos.Single(nodo => nodo.Nombre == "Nodo B");

        Assert.Equal(150, nodoDtoA.PotenciaActualW);
        Assert.Equal(200, nodoDtoB.PotenciaActualW);
        Assert.Equal(350, instantanea.Parques[0].PotenciaActualW);
    }

    [Fact]
    public void ArmarInstantanea_OrdenaParquesNodosYPaneles()
    {
        var zeta = FixtureMonitoreo.Parque("Zeta");
        var alfa = FixtureMonitoreo.Parque("Alfa");

        var nodoB = FixtureMonitoreo.Nodo(zeta, "Nodo B");
        var nodoA = FixtureMonitoreo.Nodo(zeta, "Nodo A");
        FixtureMonitoreo.Panel(nodoA, "P02");
        FixtureMonitoreo.Panel(nodoA, "P01");

        var instantanea = MonitoreoService.ArmarInstantanea([zeta, alfa], [], Ahora);

        Assert.Equal(new[] { "Alfa", "Zeta" }, instantanea.Parques.Select(parque => parque.Nombre));
        var parqueZeta = instantanea.Parques.Single(parque => parque.Nombre == "Zeta");
        Assert.Equal(new[] { "Nodo A", "Nodo B" }, parqueZeta.Nodos.Select(nodo => nodo.Nombre));
        Assert.Equal(new[] { "P01", "P02" }, parqueZeta.Nodos[0].Paneles.Select(panel => panel.CodigoPanel));
    }

    [Fact]
    public void ArmarInstantanea_UsaLaHoraRecibida()
    {
        var instantanea = MonitoreoService.ArmarInstantanea([], [], Ahora);

        Assert.Equal(Ahora, instantanea.GeneradaUtc);
        Assert.Empty(instantanea.Parques);
    }

    [Fact]
    public void ArmarInstantanea_ConservaElEstadoDeSaludDelPanel()
    {
        var parque = FixtureMonitoreo.Parque();
        var nodo = FixtureMonitoreo.Nodo(parque);
        var panel = FixtureMonitoreo.Panel(nodo, "P01", EstadoSalud.FALLA_DETECTADA);

        var instantanea = MonitoreoService.ArmarInstantanea([parque],
            [FixtureMonitoreo.Lectura(panel, Ahora, 0, 80)], Ahora);

        Assert.Equal(EstadoSalud.FALLA_DETECTADA, instantanea.Parques[0].Nodos[0].Paneles[0].Estado);
    }
}
