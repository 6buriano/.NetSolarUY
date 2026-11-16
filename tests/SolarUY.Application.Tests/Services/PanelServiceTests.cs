using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="PanelService"/>: listados con filtro de estado y nombre del nodo, lecturas
/// recientes, y el alta, modificación y baja con las reglas de nodo existente y código único por nodo.
/// </summary>
public class PanelServiceTests
{
    private static readonly DateTime Fecha = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

    private static PanelService CrearServicio(PanelRepositoryAbmFalso paneles, NodoRepositoryAbmFalso nodos,
        TelemetriaRepositoryEnMemoria? telemetria = null)
        => new(paneles, nodos, telemetria ?? new TelemetriaRepositoryEnMemoria());

    private static CrearPanelRequest PedidoDeAlta(Guid nodoId, string codigo = "P10")
        => new(codigo, "JAM72S30", "JA Solar", 550d, EstadoSalud.OPERATIVO, true, Fecha, nodoId);

    // ---------------------------------------------------------------- Consultas

    [Fact]
    public async Task ObtenerTodosAsync_SinFiltro_DevuelveTodosConElNombreDeSuNodo()
    {
        var nodo = FixtureAbm.NuevoNodo(nombre: "Nodo Central");
        var operativo = FixtureAbm.NuevoPanel(nodo.Id, "P01");
        var enFalla = FixtureAbm.NuevoPanel(nodo.Id, "P02", estado: EstadoSalud.FALLA_DETECTADA);
        var servicio = CrearServicio(new PanelRepositoryAbmFalso(operativo, enFalla),
            new NodoRepositoryAbmFalso(nodo));

        var resultado = await servicio.ObtenerTodosAsync();

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, dto => Assert.Equal("Nodo Central", dto.NodoNombre));
    }

    [Fact]
    public async Task ObtenerTodosAsync_ConFiltroDeEstado_DevuelveSoloLosDeEseEstado()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var operativo = FixtureAbm.NuevoPanel(nodo.Id, "P01");
        var enFalla = FixtureAbm.NuevoPanel(nodo.Id, "P02", estado: EstadoSalud.FALLA_DETECTADA);
        var servicio = CrearServicio(new PanelRepositoryAbmFalso(operativo, enFalla),
            new NodoRepositoryAbmFalso(nodo));

        var resultado = await servicio.ObtenerTodosAsync(EstadoSalud.FALLA_DETECTADA);

        var dto = Assert.Single(resultado);
        Assert.Equal("P02", dto.CodigoPanel);
        Assert.Equal(EstadoSalud.FALLA_DETECTADA, dto.Estado);
    }

    [Fact]
    public async Task ObtenerTodosAsync_SiElNodoNoTieneNombreConocido_DejaElNombreNulo()
    {
        var panel = FixtureAbm.NuevoPanel(Guid.NewGuid());
        var servicio = CrearServicio(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso());

        var dto = Assert.Single(await servicio.ObtenerTodosAsync());

        Assert.Null(dto.NodoNombre);
    }

    [Fact]
    public async Task ObtenerPorNodoAsync_DevuelveSoloLosPanelesDelNodoConSuNombre()
    {
        var nodo = FixtureAbm.NuevoNodo(nombre: "Nodo Este");
        var otro = FixtureAbm.NuevoNodo(nombre: "Nodo Oeste", numeroSerie: "SN-002");
        var propio = FixtureAbm.NuevoPanel(nodo.Id, "P01");
        var ajeno = FixtureAbm.NuevoPanel(otro.Id, "P01");
        var servicio = CrearServicio(new PanelRepositoryAbmFalso(propio, ajeno),
            new NodoRepositoryAbmFalso(nodo, otro));

        var resultado = await servicio.ObtenerPorNodoAsync(nodo.Id);

        var dto = Assert.Single(resultado);
        Assert.Equal(propio.Id, dto.Id);
        Assert.Equal("Nodo Este", dto.NodoNombre);
    }

    [Fact]
    public async Task ObtenerPorNodoAsync_ConFiltroDeEstado_LoPasaAlRepositorio()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var operativo = FixtureAbm.NuevoPanel(nodo.Id, "P01");
        var desconectado = FixtureAbm.NuevoPanel(nodo.Id, "P02", estado: EstadoSalud.DESCONECTADO);
        var servicio = CrearServicio(new PanelRepositoryAbmFalso(operativo, desconectado),
            new NodoRepositoryAbmFalso(nodo));

        var resultado = await servicio.ObtenerPorNodoAsync(nodo.Id, EstadoSalud.DESCONECTADO);

        Assert.Equal("P02", Assert.Single(resultado).CodigoPanel);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_SiExiste_DevuelveElPanelConElNombreDelNodo()
    {
        var nodo = FixtureAbm.NuevoNodo(nombre: "Nodo Sur");
        var panel = FixtureAbm.NuevoPanel(nodo.Id);
        var servicio = CrearServicio(new PanelRepositoryAbmFalso(panel), new NodoRepositoryAbmFalso(nodo));

        var dto = await servicio.ObtenerPorIdAsync(panel.Id);

        Assert.NotNull(dto);
        Assert.Equal(panel.Id, dto.Id);
        Assert.Equal("Nodo Sur", dto.NodoNombre);
        Assert.Equal(550d, dto.PotenciaNominalW, 6);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_SiNoExiste_DevuelveNulo()
    {
        var servicio = CrearServicio(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso());

        Assert.Null(await servicio.ObtenerPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObtenerLecturasRecientesAsync_PideLaCantidadAlRepositorioYConvierteLasLecturas()
    {
        var panelId = Guid.NewGuid();
        var lectura = new LecturaTelemetria(panelId, Fecha, 8.5d, 40d, 340d, 45d);
        var telemetria = new TelemetriaRepositoryEnMemoria { Lecturas = [lectura] };
        var servicio = CrearServicio(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso(), telemetria);

        var resultado = await servicio.ObtenerLecturasRecientesAsync(panelId, 20);

        Assert.Equal(panelId, telemetria.UltimoPanelId);
        Assert.Equal(20, telemetria.UltimaCantidadPedida);
        var dto = Assert.Single(resultado);
        Assert.Equal(lectura.Id, dto.Id);
        Assert.Equal(panelId, dto.PanelSolarId);
        Assert.Equal(Fecha, dto.FechaHora);
        Assert.Equal(340d, dto.PotenciaGeneradaWatts, 6);
        Assert.Equal(45d, dto.TemperaturaCelsius, 6);
    }

    [Fact]
    public async Task ObtenerLecturasRecientesAsync_SinLecturas_DevuelveListaVacia()
    {
        var servicio = CrearServicio(new PanelRepositoryAbmFalso(), new NodoRepositoryAbmFalso());

        Assert.Empty(await servicio.ObtenerLecturasRecientesAsync(Guid.NewGuid(), 5));
    }

    // ---------------------------------------------------------------- Alta

    [Fact]
    public async Task CrearAsync_GuardaElPanelYDevuelveSuId()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var paneles = new PanelRepositoryAbmFalso();
        var servicio = CrearServicio(paneles, new NodoRepositoryAbmFalso(nodo));

        var id = await servicio.CrearAsync(PedidoDeAlta(nodo.Id, "P10"));

        var guardado = Assert.Single(paneles.Agregados);
        Assert.Equal(id, guardado.Id);
        Assert.Equal("P10", guardado.CodigoPanel);
        Assert.Equal(nodo.Id, guardado.NodoId);
        Assert.Null(paneles.PanelExcluidoEnCodigo);
    }

    [Fact]
    public async Task CrearAsync_SiElNodoNoExiste_LanzaReglaDeNegocioYNoGuarda()
    {
        var paneles = new PanelRepositoryAbmFalso();
        var servicio = CrearServicio(paneles, new NodoRepositoryAbmFalso());

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.CrearAsync(PedidoDeAlta(Guid.NewGuid())));

        Assert.Equal("El nodo seleccionado no existe.", error.Message);
        Assert.Empty(paneles.Agregados);
    }

    [Fact]
    public async Task CrearAsync_SiElCodigoYaExisteEnElNodo_LanzaReglaDeNegocioYNoGuarda()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var paneles = new PanelRepositoryAbmFalso { CodigoOcupado = true };
        var servicio = CrearServicio(paneles, new NodoRepositoryAbmFalso(nodo));

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.CrearAsync(PedidoDeAlta(nodo.Id, "P10")));

        Assert.Contains("'P10'", error.Message);
        Assert.Empty(paneles.Agregados);
    }

    // ---------------------------------------------------------------- Modificación

    [Fact]
    public async Task ActualizarAsync_SiExiste_CambiaLosDatosYLoGuarda()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var panel = FixtureAbm.NuevoPanel(nodo.Id, "P01");
        var paneles = new PanelRepositoryAbmFalso(panel);
        var servicio = CrearServicio(paneles, new NodoRepositoryAbmFalso(nodo));

        var resultado = await servicio.ActualizarAsync(new ActualizarPanelRequest(panel.Id, "P99", "Otro modelo",
            "Otro fabricante", 600d, EstadoSalud.RENDIMIENTO_BAJO, false, Fecha, nodo.Id));

        Assert.True(resultado);
        Assert.Same(panel, Assert.Single(paneles.Actualizados));
        Assert.Equal("P99", panel.CodigoPanel);
        Assert.Equal(600d, panel.PotenciaNominalW, 6);
        Assert.Equal(EstadoSalud.RENDIMIENTO_BAJO, panel.Estado);
        Assert.False(panel.Activo);
        // En la edición el propio panel no cuenta como código repetido.
        Assert.Equal(panel.Id, paneles.PanelExcluidoEnCodigo);
    }

    [Fact]
    public async Task ActualizarAsync_SiNoExiste_DevuelveFalsoYNoGuarda()
    {
        var paneles = new PanelRepositoryAbmFalso();
        var servicio = CrearServicio(paneles, new NodoRepositoryAbmFalso());

        var resultado = await servicio.ActualizarAsync(new ActualizarPanelRequest(Guid.NewGuid(), "P01", "M", "F",
            500d, EstadoSalud.OPERATIVO, true, Fecha, Guid.NewGuid()));

        Assert.False(resultado);
        Assert.Empty(paneles.Actualizados);
    }

    [Fact]
    public async Task ActualizarAsync_SiElNuevoNodoNoExiste_LanzaReglaDeNegocio()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var panel = FixtureAbm.NuevoPanel(nodo.Id);
        var paneles = new PanelRepositoryAbmFalso(panel);
        var servicio = CrearServicio(paneles, new NodoRepositoryAbmFalso(nodo));

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => servicio.ActualizarAsync(
            new ActualizarPanelRequest(panel.Id, "P01", "M", "F", 500d, EstadoSalud.OPERATIVO, true, Fecha,
                Guid.NewGuid())));

        Assert.Equal("El nodo seleccionado no existe.", error.Message);
        Assert.Empty(paneles.Actualizados);
    }

    [Fact]
    public async Task ActualizarAsync_SiElCodigoYaExisteEnElNodo_LanzaReglaDeNegocio()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var panel = FixtureAbm.NuevoPanel(nodo.Id);
        var paneles = new PanelRepositoryAbmFalso(panel) { CodigoOcupado = true };
        var servicio = CrearServicio(paneles, new NodoRepositoryAbmFalso(nodo));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => servicio.ActualizarAsync(
            new ActualizarPanelRequest(panel.Id, "P02", "M", "F", 500d, EstadoSalud.OPERATIVO, true, Fecha, nodo.Id)));

        Assert.Empty(paneles.Actualizados);
    }

    // ---------------------------------------------------------------- Baja

    [Fact]
    public async Task EliminarAsync_SiExiste_LoElimina()
    {
        var panel = FixtureAbm.NuevoPanel(Guid.NewGuid());
        var paneles = new PanelRepositoryAbmFalso(panel);
        var servicio = CrearServicio(paneles, new NodoRepositoryAbmFalso());

        var resultado = await servicio.EliminarAsync(panel.Id);

        Assert.True(resultado);
        Assert.Same(panel, Assert.Single(paneles.Eliminados));
    }

    [Fact]
    public async Task EliminarAsync_SiNoExiste_DevuelveFalso()
    {
        var paneles = new PanelRepositoryAbmFalso();
        var servicio = CrearServicio(paneles, new NodoRepositoryAbmFalso());

        Assert.False(await servicio.EliminarAsync(Guid.NewGuid()));
        Assert.Empty(paneles.Eliminados);
    }
}
