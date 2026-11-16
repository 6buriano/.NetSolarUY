using SolarUY.Application.Sensors;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="VerificacionVinculacionService"/>: pide el inventario al simulador y, si responde,
/// lo compara con los nodos del sistema; si no responde, devuelve un informe que lo explica.
/// </summary>
public class VerificacionVinculacionServiceTests
{
    private const string UrlSimulador = "http://localhost:5100";

    [Fact]
    public async Task VerificarAsync_SiElSimuladorNoResponde_DevuelveUnInformeConElMotivo()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var cliente = new SimuladorAdminFalso(null);
        var servicio = new VerificacionVinculacionService(new NodoRepositoryAbmFalso(nodo), cliente);

        var informe = await servicio.VerificarAsync(UrlSimulador);

        Assert.False(informe.SimuladorConsultado);
        Assert.NotNull(informe.ErrorConsulta);
        Assert.Contains(UrlSimulador, informe.ErrorConsulta);
        Assert.Contains("docker compose up -d sensor-simulator", informe.ErrorConsulta);
        Assert.Equal(1, informe.NodosRevisados);
        Assert.Equal(UrlSimulador, cliente.UrlPedida);
    }

    [Fact]
    public async Task VerificarAsync_SiElSimuladorResponde_ComparaConLosNodosDelSistema()
    {
        var nodo = FixtureAbm.NuevoNodo(hardware: "ARM-001");
        nodo.Paneles.Add(FixtureAbm.NuevoPanel(nodo.Id, "P01"));
        IReadOnlyList<InventarioSensorDto> inventario =
        [
            new("ARM-001", true, [new InventarioPanelDto("P01", 550d, true, null, false)]),
            new("ARM-002", true, [])
        ];
        var servicio = new VerificacionVinculacionService(new NodoRepositoryAbmFalso(nodo),
            new SimuladorAdminFalso(inventario));

        var informe = await servicio.VerificarAsync(UrlSimulador);

        Assert.True(informe.SimuladorConsultado);
        Assert.True(informe.TodoVinculado);
        Assert.Equal(1, informe.NodosRevisados);
        Assert.Equal(1, informe.PanelesRevisados);
        Assert.Equal("ARM-002", Assert.Single(informe.SensoresLibres));
    }

    [Fact]
    public async Task VerificarAsync_SiHayDiscrepancias_LasIncluyeEnElInforme()
    {
        var nodo = FixtureAbm.NuevoNodo(nombre: "Nodo Huérfano");
        var servicio = new VerificacionVinculacionService(new NodoRepositoryAbmFalso(nodo),
            new SimuladorAdminFalso([]));

        var informe = await servicio.VerificarAsync(UrlSimulador);

        var hallazgo = Assert.Single(informe.Hallazgos);
        Assert.Equal("Nodo Huérfano", hallazgo.Nodo);
        Assert.Equal(1, informe.Errores);
    }

    private sealed class SimuladorAdminFalso(IReadOnlyList<InventarioSensorDto>? inventario) : ISimuladorAdminClient
    {
        public string? UrlPedida { get; private set; }

        public Task<IReadOnlyList<InventarioSensorDto>?> ObtenerInventarioAsync(string urlBase,
            CancellationToken cancellationToken = default)
        {
            UrlPedida = urlBase;
            return Task.FromResult(inventario);
        }

        public Task<InventarioSensorDto?> CrearSensorAsync(string urlBase, string identificadorHardwareARM,
            int cantidadPaneles, double potenciaNominalW, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
