using SolarUY.Web.Hubs;

namespace SolarUY.Web.Tests;

/// <summary>Pruebas de <see cref="MonitoreoHub"/>: a qué grupo se suscribe cada conexión.</summary>
public class MonitoreoHubTests
{
    private static (MonitoreoHub Hub, GruposFalsos Grupos) CrearHub()
    {
        var grupos = new GruposFalsos();
        var hub = new MonitoreoHub
        {
            Context = new ContextoDeLlamadaFalso("conexion-1"),
            Groups = grupos
        };

        return (hub, grupos);
    }

    [Fact]
    public async Task SuscribirseMonitoreo_AgregaLaConexionAlGrupoDeMonitoreo()
    {
        var (hub, grupos) = CrearHub();

        await hub.SuscribirseMonitoreo();

        Assert.Equal(("conexion-1", MonitoreoHub.GrupoMonitoreo), Assert.Single(grupos.Agregados));
    }

    [Fact]
    public async Task SuscribirsePanel_AgregaLaConexionAlGrupoDeEsePanel()
    {
        var (hub, grupos) = CrearHub();
        var panelId = Guid.NewGuid();

        await hub.SuscribirsePanel(panelId);

        Assert.Equal(("conexion-1", $"panel-{panelId}"), Assert.Single(grupos.Agregados));
    }

    [Fact]
    public void GrupoPanel_ArmaElNombreConElIdDelPanel()
    {
        var panelId = Guid.NewGuid();

        Assert.Equal($"panel-{panelId}", MonitoreoHub.GrupoPanel(panelId));
    }
}
