using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.Application.Sensors;
using SolarUY.Infrastructure.Sensors;

namespace SolarUY.Infrastructure.Tests.Sensors;

/// <summary>
/// Pruebas de <see cref="SensorDisponibilidadSimulador"/> (pregunta al simulador qué sensores hay y da de alta
/// los que faltan) y de <see cref="SensorDisponibilidadNula"/> (el entorno sin simulador).
/// </summary>
public class SensorDisponibilidadTests
{
    private const string UrlSimulador = "http://simulador:5100";

    private static InventarioSensorDto Sensor(string identificador, int paneles = 2, double potenciaW = 550d)
        => new(identificador, true,
            Enumerable.Range(1, paneles)
                .Select(i => new InventarioPanelDto($"P{i:D2}", potenciaW, true, null, false))
                .ToList());

    private static SensorDisponibilidadSimulador Crear(SimuladorAdminFalso simulador)
        => new(simulador, UrlSimulador, NullLogger<SensorDisponibilidadSimulador>.Instance);

    // ---------------------------------------------------------------- Verificar

    [Fact]
    public async Task Verificar_SiElSimuladorNoResponde_DiceQueNoSePudoConsultar()
    {
        var disponibilidad = await Crear(new SimuladorAdminFalso { Inventario = null }).VerificarAsync("ARM-001");

        Assert.True(disponibilidad.VerificacionPosible);
        Assert.False(disponibilidad.SePudoConsultar);
        Assert.False(disponibilidad.Existe);
        Assert.Empty(disponibilidad.IdentificadoresExistentes);
    }

    [Fact]
    public async Task Verificar_SiElSensorExiste_NoDistingueMayusculas()
    {
        var simulador = new SimuladorAdminFalso { Inventario = [Sensor("ARM-001"), Sensor("ARM-002")] };

        var disponibilidad = await Crear(simulador).VerificarAsync("arm-002");

        Assert.True(disponibilidad.SePudoConsultar);
        Assert.True(disponibilidad.Existe);
        Assert.Equal(2, disponibilidad.IdentificadoresExistentes.Count);
        Assert.Equal(UrlSimulador, simulador.UrlConsultada);
    }

    [Fact]
    public async Task Verificar_SiElSensorNoExiste_ListaLosQueSiExisten()
    {
        var simulador = new SimuladorAdminFalso { Inventario = [Sensor("ARM-001")] };

        var disponibilidad = await Crear(simulador).VerificarAsync("ARM-099");

        Assert.True(disponibilidad.SePudoConsultar);
        Assert.False(disponibilidad.Existe);
        Assert.Equal("ARM-001", Assert.Single(disponibilidad.IdentificadoresExistentes));
    }

    // ---------------------------------------------------------------- Crear

    [Fact]
    public async Task Crear_CopiaLaTopologiaDelPrimerSensorExistente()
    {
        var simulador = new SimuladorAdminFalso
        {
            Inventario = [Sensor("ARM-001", paneles: 4, potenciaW: 600d)],
            Creado = Sensor("ARM-009", paneles: 4, potenciaW: 600d)
        };

        var resultado = await Crear(simulador).CrearAsync("ARM-009");

        Assert.True(resultado.Creado);
        Assert.Null(resultado.Error);
        Assert.Equal(["P01", "P02", "P03", "P04"], resultado.CodigosDePanel.ToArray());
        Assert.Equal("ARM-009", simulador.UltimoAlta!.Identificador);
        Assert.Equal(4, simulador.UltimoAlta.CantidadPaneles);
        Assert.Equal(600d, simulador.UltimoAlta.PotenciaNominalW, 6);
        Assert.Equal(UrlSimulador, simulador.UltimoAlta.Url);
    }

    [Fact]
    public async Task Crear_SinSensoresDeReferencia_UsaLosValoresPorDefecto()
    {
        var simulador = new SimuladorAdminFalso { Inventario = [], Creado = Sensor("ARM-009", 5, 300d) };

        await Crear(simulador).CrearAsync("ARM-009");

        Assert.Equal(5, simulador.UltimoAlta!.CantidadPaneles);
        Assert.Equal(300d, simulador.UltimoAlta.PotenciaNominalW, 6);
    }

    [Fact]
    public async Task Crear_SiElSimuladorNoDaElInventario_UsaLosValoresPorDefecto()
    {
        var simulador = new SimuladorAdminFalso { Inventario = null, Creado = Sensor("ARM-009", 5, 300d) };

        await Crear(simulador).CrearAsync("ARM-009");

        Assert.Equal(5, simulador.UltimoAlta!.CantidadPaneles);
        Assert.Equal(300d, simulador.UltimoAlta.PotenciaNominalW, 6);
    }

    [Fact]
    public async Task Crear_SiElPrimerSensorNoTienePaneles_UsaLaCantidadPorDefecto()
    {
        var simulador = new SimuladorAdminFalso { Inventario = [Sensor("ARM-001", paneles: 0)], Creado = Sensor("ARM-009") };

        await Crear(simulador).CrearAsync("ARM-009");

        Assert.Equal(5, simulador.UltimoAlta!.CantidadPaneles);
        Assert.Equal(300d, simulador.UltimoAlta.PotenciaNominalW, 6);
    }

    [Fact]
    public async Task Crear_SiElSimuladorNoLoCrea_DevuelveElError()
    {
        var simulador = new SimuladorAdminFalso { Inventario = [Sensor("ARM-001")], Creado = null };

        var resultado = await Crear(simulador).CrearAsync("ARM-009");

        Assert.False(resultado.Creado);
        Assert.Contains("No se pudo crear el sensor 'ARM-009'", resultado.Error);
        Assert.Empty(resultado.CodigosDePanel);
    }

    // ---------------------------------------------------------------- Entorno sin simulador

    [Fact]
    public async Task Nula_NoPuedeVerificarNada()
    {
        var disponibilidad = await new SensorDisponibilidadNula().VerificarAsync("ARM-001");

        Assert.False(disponibilidad.VerificacionPosible);
        Assert.False(disponibilidad.SePudoConsultar);
        Assert.False(disponibilidad.Existe);
        Assert.Empty(disponibilidad.IdentificadoresExistentes);
    }

    [Fact]
    public async Task Nula_NoPuedeCrearSensores()
    {
        var resultado = await new SensorDisponibilidadNula().CrearAsync("ARM-001");

        Assert.False(resultado.Creado);
        Assert.Contains("no puede crear sensores", resultado.Error);
        Assert.Empty(resultado.CodigosDePanel);
    }

    private sealed record AltaPedida(string Url, string Identificador, int CantidadPaneles, double PotenciaNominalW);

    private sealed class SimuladorAdminFalso : ISimuladorAdminClient
    {
        public IReadOnlyList<InventarioSensorDto>? Inventario { get; init; }
        public InventarioSensorDto? Creado { get; init; }

        public string? UrlConsultada { get; private set; }
        public AltaPedida? UltimoAlta { get; private set; }

        public Task<IReadOnlyList<InventarioSensorDto>?> ObtenerInventarioAsync(string urlBase,
            CancellationToken cancellationToken = default)
        {
            UrlConsultada = urlBase;
            return Task.FromResult(Inventario);
        }

        public Task<InventarioSensorDto?> CrearSensorAsync(string urlBase, string identificadorHardwareARM,
            int cantidadPaneles, double potenciaNominalW, CancellationToken cancellationToken = default)
        {
            UltimoAlta = new AltaPedida(urlBase, identificadorHardwareARM, cantidadPaneles, potenciaNominalW);
            return Task.FromResult(Creado);
        }
    }
}
