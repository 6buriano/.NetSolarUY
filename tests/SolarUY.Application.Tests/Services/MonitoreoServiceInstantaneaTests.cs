using SolarUY.Application.Services;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="MonitoreoService.ObtenerInstantaneaAsync"/>, la parte que sí consulta los
/// repositorios. El armado de la foto en sí (<c>ArmarInstantanea</c>) se prueba en
/// <see cref="MonitoreoServiceTests"/>.
/// </summary>
public class MonitoreoServiceInstantaneaTests
{
    [Fact]
    public async Task ObtenerInstantaneaAsync_PideLasLecturasDeLaVentanaDeLosUltimosSegundos()
    {
        var parque = FixtureMonitoreo.Parque();
        var telemetria = new TelemetriaRepositoryEnMemoria();
        var servicio = new MonitoreoService(new ParqueRepositoryEnMemoria(parque), telemetria);

        var antes = DateTime.UtcNow;
        await servicio.ObtenerInstantaneaAsync();
        var despues = DateTime.UtcNow;

        Assert.NotNull(telemetria.DesdeUtcPedido);
        var pedido = telemetria.DesdeUtcPedido.Value;
        var ventana = TimeSpan.FromSeconds(MonitoreoService.VentanaSegundos);
        Assert.InRange(pedido, antes - ventana, despues - ventana);
    }

    [Fact]
    public async Task ObtenerInstantaneaAsync_DevuelveLaFotoDeLosParquesYLasLecturasDeLaVentana()
    {
        var parque = FixtureMonitoreo.Parque("Parque Sur");
        var nodo = FixtureMonitoreo.Nodo(parque);
        var panel = FixtureMonitoreo.Panel(nodo, "P01");
        var lectura = FixtureMonitoreo.Lectura(panel, DateTime.UtcNow.AddSeconds(-2), 250);
        var telemetria = new TelemetriaRepositoryEnMemoria { Lecturas = [lectura] };
        var servicio = new MonitoreoService(new ParqueRepositoryEnMemoria(parque), telemetria);

        var (instantanea, lecturas) = await servicio.ObtenerInstantaneaAsync();

        Assert.Same(lectura, Assert.Single(lecturas));
        var parqueDto = Assert.Single(instantanea.Parques);
        Assert.Equal("Parque Sur", parqueDto.Nombre);
        Assert.Equal(250, parqueDto.PotenciaActualW);
    }

    [Fact]
    public async Task ObtenerInstantaneaAsync_SinParques_DevuelveUnaFotoVacia()
    {
        var servicio = new MonitoreoService(new ParqueRepositoryEnMemoria(), new TelemetriaRepositoryEnMemoria());

        var (instantanea, lecturas) = await servicio.ObtenerInstantaneaAsync();

        Assert.Empty(instantanea.Parques);
        Assert.Empty(lecturas);
    }
}
