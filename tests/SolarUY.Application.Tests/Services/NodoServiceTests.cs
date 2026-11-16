using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="NodoService"/>: un nodo se carga siempre con su inversor y, opcionalmente, con su
/// sensor. Se prueban las consultas con el nombre del parque, las validaciones de unicidad y los cuatro
/// caminos de la edición del sensor (agregar, actualizar, quitar y dejarlo como está).
/// </summary>
public class NodoServiceTests
{
    private static NodoService CrearServicio(NodoRepositoryAbmFalso nodos, ParqueRepositoryAbmFalso parques)
        => new(nodos, parques);

    private static InversorRequest InversorValido(string numeroSerie = "SN-100")
        => new("Huawei SUN2000", numeroSerie, 97.5d, true);

    private static SensorDispositivoRequest SensorValido(string hardware = "ARM-100")
        => new(hardware, "2.1.0", "http://localhost:5100", true);

    private static CrearNodoRequest PedidoDeAlta(Guid parqueId, SensorDispositivoRequest? sensor = null,
        string numeroSerie = "SN-100")
        => new("  Nodo Nuevo ", "Sector 7", parqueId, InversorValido(numeroSerie), sensor);

    private static ActualizarNodoRequest PedidoDeEdicion(Nodo nodo, SensorDispositivoRequest? sensor,
        string numeroSerie = "SN-200")
        => new(nodo.Id, "Nodo Renombrado", "Sector 9", nodo.ParqueSolarId, InversorValido(numeroSerie), sensor);

    // ---------------------------------------------------------------- Consultas

    [Fact]
    public async Task ObtenerTodosAsync_DevuelveLosNodosConElNombreDeSuParque()
    {
        var parque = FixtureAbm.NuevoParque("Parque Litoral");
        var nodo = FixtureAbm.NuevoNodo(parque.Id, hardware: "ARM-001");
        nodo.Paneles.Add(FixtureAbm.NuevoPanel(nodo.Id, "P01"));
        nodo.Paneles.Add(FixtureAbm.NuevoPanel(nodo.Id, "P02"));
        var servicio = CrearServicio(new NodoRepositoryAbmFalso(nodo), new ParqueRepositoryAbmFalso(parque));

        var dto = Assert.Single(await servicio.ObtenerTodosAsync());

        Assert.Equal(nodo.Id, dto.Id);
        Assert.Equal("Parque Litoral", dto.ParqueNombre);
        Assert.Equal(2, dto.CantidadPaneles);
        Assert.Equal("SN-001", dto.Inversor.NumeroSerie);
        Assert.NotNull(dto.Inversor.Sensor);
        Assert.Equal("ARM-001", dto.Inversor.Sensor.IdentificadorHardwareARM);
    }

    [Fact]
    public async Task ObtenerTodosAsync_SiElParqueNoEstaRegistrado_DejaElNombreNulo()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var servicio = CrearServicio(new NodoRepositoryAbmFalso(nodo), new ParqueRepositoryAbmFalso());

        var dto = Assert.Single(await servicio.ObtenerTodosAsync());

        Assert.Null(dto.ParqueNombre);
        Assert.Null(dto.Inversor.Sensor);
    }

    [Fact]
    public async Task ObtenerPorParqueAsync_DevuelveSoloLosNodosDeEseParque()
    {
        var parque = FixtureAbm.NuevoParque("Parque Este");
        var propio = FixtureAbm.NuevoNodo(parque.Id, "Nodo Propio");
        var ajeno = FixtureAbm.NuevoNodo(Guid.NewGuid(), "Nodo Ajeno", "SN-002");
        var servicio = CrearServicio(new NodoRepositoryAbmFalso(propio, ajeno), new ParqueRepositoryAbmFalso(parque));

        var resultado = await servicio.ObtenerPorParqueAsync(parque.Id);

        var dto = Assert.Single(resultado);
        Assert.Equal("Nodo Propio", dto.Nombre);
        Assert.Equal("Parque Este", dto.ParqueNombre);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_SiExiste_DevuelveElNodoConElNombreDelParque()
    {
        var parque = FixtureAbm.NuevoParque("Parque Oeste");
        var nodo = FixtureAbm.NuevoNodo(parque.Id);
        var servicio = CrearServicio(new NodoRepositoryAbmFalso(nodo), new ParqueRepositoryAbmFalso(parque));

        var dto = await servicio.ObtenerPorIdAsync(nodo.Id);

        Assert.NotNull(dto);
        Assert.Equal(nodo.Id, dto.Id);
        Assert.Equal("Parque Oeste", dto.ParqueNombre);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_SiNoExiste_DevuelveNulo()
    {
        var servicio = CrearServicio(new NodoRepositoryAbmFalso(), new ParqueRepositoryAbmFalso());

        Assert.Null(await servicio.ObtenerPorIdAsync(Guid.NewGuid()));
    }

    // ---------------------------------------------------------------- Alta

    [Fact]
    public async Task CrearAsync_SinSensor_GuardaElNodoConSuInversor()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodos = new NodoRepositoryAbmFalso();
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));

        var id = await servicio.CrearAsync(PedidoDeAlta(parque.Id));

        var guardado = Assert.Single(nodos.Agregados);
        Assert.Equal(id, guardado.Id);
        Assert.Equal("Nodo Nuevo", guardado.Nombre);
        Assert.Equal(parque.Id, guardado.ParqueSolarId);
        Assert.Equal("SN-100", guardado.Inversor.NumeroSerie);
        Assert.Null(guardado.Inversor.Sensor);
        // Sin sensor no hay identificador de hardware que validar.
        Assert.False(nodos.SePreguntoPorIdentificador);
    }

    [Fact]
    public async Task CrearAsync_ConSensor_LoRegistraEnElInversor()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodos = new NodoRepositoryAbmFalso();
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));

        await servicio.CrearAsync(PedidoDeAlta(parque.Id, SensorValido("ARM-777")));

        var guardado = Assert.Single(nodos.Agregados);
        var sensor = guardado.Inversor.Sensor;
        Assert.NotNull(sensor);
        Assert.Equal("ARM-777", sensor.IdentificadorHardwareARM);
        Assert.Equal("2.1.0", sensor.VersionFirmware);
        Assert.Equal("http://localhost:5100", sensor.UrlBase);
        Assert.True(sensor.Activo);
        Assert.Equal(guardado.Inversor.Id, sensor.InversorId);
        Assert.True(nodos.SePreguntoPorIdentificador);
        Assert.Null(nodos.NodoExcluidoEnIdentificador);
    }

    [Fact]
    public async Task CrearAsync_SiElParqueNoExiste_LanzaReglaDeNegocioYNoGuarda()
    {
        var nodos = new NodoRepositoryAbmFalso();
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso());

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.CrearAsync(PedidoDeAlta(Guid.NewGuid())));

        Assert.Equal("El parque solar seleccionado no existe.", error.Message);
        Assert.Empty(nodos.Agregados);
    }

    [Fact]
    public async Task CrearAsync_SiElNumeroDeSerieYaExiste_LanzaReglaDeNegocioYNoGuarda()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodos = new NodoRepositoryAbmFalso { NumeroSerieOcupado = true };
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.CrearAsync(PedidoDeAlta(parque.Id, numeroSerie: "SN-DUP")));

        Assert.Contains("'SN-DUP'", error.Message);
        Assert.Empty(nodos.Agregados);
    }

    [Fact]
    public async Task CrearAsync_SiElIdentificadorDeHardwareYaExiste_LanzaReglaDeNegocioYNoGuarda()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodos = new NodoRepositoryAbmFalso { IdentificadorOcupado = true };
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.CrearAsync(PedidoDeAlta(parque.Id, SensorValido("ARM-DUP"))));

        Assert.Contains("'ARM-DUP'", error.Message);
        Assert.Empty(nodos.Agregados);
    }

    [Fact]
    public async Task CrearAsync_SiLosDatosDelNodoSonInvalidos_LanzaReglaDeNegocioYNoGuarda()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodos = new NodoRepositoryAbmFalso();
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));
        var pedido = new CrearNodoRequest("   ", "Sector 7", parque.Id, InversorValido(), null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => servicio.CrearAsync(pedido));

        Assert.Empty(nodos.Agregados);
    }

    // ---------------------------------------------------------------- Modificación

    [Fact]
    public async Task ActualizarAsync_SiNoExiste_DevuelveFalsoYNoGuarda()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodos = new NodoRepositoryAbmFalso();
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));
        var pedido = new ActualizarNodoRequest(Guid.NewGuid(), "Nodo", "Sector", parque.Id, InversorValido(), null);

        Assert.False(await servicio.ActualizarAsync(pedido));
        Assert.Empty(nodos.Actualizados);
    }

    [Fact]
    public async Task ActualizarAsync_CambiaLosDatosDelNodoYDelInversor()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodo = FixtureAbm.NuevoNodo(parque.Id);
        var nodos = new NodoRepositoryAbmFalso(nodo);
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));

        var resultado = await servicio.ActualizarAsync(PedidoDeEdicion(nodo, null, "SN-200"));

        Assert.True(resultado);
        Assert.Same(nodo, Assert.Single(nodos.Actualizados));
        Assert.Equal("Nodo Renombrado", nodo.Nombre);
        Assert.Equal("Sector 9", nodo.UbicacionDentroParque);
        Assert.Equal("SN-200", nodo.Inversor.NumeroSerie);
        Assert.Equal(97.5d, nodo.Inversor.EficienciaConversion, 6);
        // En la edición el propio nodo no cuenta como número de serie repetido.
        Assert.Equal(nodo.Id, nodos.NodoExcluidoEnNumeroSerie);
        Assert.Null(nodos.SensorNuevoInformado);
    }

    [Fact]
    public async Task ActualizarAsync_SiElNodoNoTeniaSensorYSeInforma_LoAgregaYLoAvisaAlRepositorio()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodo = FixtureAbm.NuevoNodo(parque.Id);
        var nodos = new NodoRepositoryAbmFalso(nodo);
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));

        await servicio.ActualizarAsync(PedidoDeEdicion(nodo, SensorValido("ARM-555")));

        Assert.NotNull(nodo.Inversor.Sensor);
        Assert.Equal("ARM-555", nodo.Inversor.Sensor.IdentificadorHardwareARM);
        // El repositorio necesita saber que ese sensor es nuevo para insertarlo y no actualizarlo.
        Assert.Same(nodo.Inversor.Sensor, nodos.SensorNuevoInformado);
        Assert.Equal(nodo.Id, nodos.NodoExcluidoEnIdentificador);
    }

    [Fact]
    public async Task ActualizarAsync_SiElNodoTeniaSensorYSeInforma_LoActualizaSinMarcarloComoNuevo()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodo = FixtureAbm.NuevoNodo(parque.Id, hardware: "ARM-001");
        var sensorOriginal = nodo.Inversor.Sensor;
        var nodos = new NodoRepositoryAbmFalso(nodo);
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));

        await servicio.ActualizarAsync(PedidoDeEdicion(nodo, SensorValido("ARM-002")));

        Assert.Same(sensorOriginal, nodo.Inversor.Sensor);
        Assert.NotNull(nodo.Inversor.Sensor);
        Assert.Equal("ARM-002", nodo.Inversor.Sensor.IdentificadorHardwareARM);
        Assert.Equal("2.1.0", nodo.Inversor.Sensor.VersionFirmware);
        Assert.Null(nodos.SensorNuevoInformado);
    }

    [Fact]
    public async Task ActualizarAsync_SiElNodoTeniaSensorYNoSeInforma_LoQuita()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodo = FixtureAbm.NuevoNodo(parque.Id, hardware: "ARM-001");
        var nodos = new NodoRepositoryAbmFalso(nodo);
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));

        await servicio.ActualizarAsync(PedidoDeEdicion(nodo, null));

        Assert.Null(nodo.Inversor.Sensor);
        Assert.Null(nodos.SensorNuevoInformado);
        Assert.False(nodos.SePreguntoPorIdentificador);
        Assert.Single(nodos.Actualizados);
    }

    [Fact]
    public async Task ActualizarAsync_SiElParqueNoExiste_LanzaReglaDeNegocioYNoGuarda()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var nodos = new NodoRepositoryAbmFalso(nodo);
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso());

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.ActualizarAsync(PedidoDeEdicion(nodo, null)));

        Assert.Empty(nodos.Actualizados);
    }

    [Fact]
    public async Task ActualizarAsync_SiElNumeroDeSerieYaLoTieneOtroNodo_LanzaReglaDeNegocio()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodo = FixtureAbm.NuevoNodo(parque.Id);
        var nodos = new NodoRepositoryAbmFalso(nodo) { NumeroSerieOcupado = true };
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.ActualizarAsync(PedidoDeEdicion(nodo, null)));

        Assert.Empty(nodos.Actualizados);
    }

    [Fact]
    public async Task ActualizarAsync_SiElIdentificadorDeHardwareYaLoTieneOtroNodo_LanzaReglaDeNegocio()
    {
        var parque = FixtureAbm.NuevoParque();
        var nodo = FixtureAbm.NuevoNodo(parque.Id);
        var nodos = new NodoRepositoryAbmFalso(nodo) { IdentificadorOcupado = true };
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso(parque));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.ActualizarAsync(PedidoDeEdicion(nodo, SensorValido("ARM-DUP"))));

        Assert.Empty(nodos.Actualizados);
    }

    // ---------------------------------------------------------------- Baja

    [Fact]
    public async Task EliminarAsync_SiNoTienePaneles_LoElimina()
    {
        var nodo = FixtureAbm.NuevoNodo();
        var nodos = new NodoRepositoryAbmFalso(nodo);
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso());

        var resultado = await servicio.EliminarAsync(nodo.Id);

        Assert.True(resultado);
        Assert.Same(nodo, Assert.Single(nodos.Eliminados));
    }

    [Fact]
    public async Task EliminarAsync_SiNoExiste_DevuelveFalso()
    {
        var nodos = new NodoRepositoryAbmFalso();
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso());

        Assert.False(await servicio.EliminarAsync(Guid.NewGuid()));
        Assert.Empty(nodos.Eliminados);
    }

    [Fact]
    public async Task EliminarAsync_SiTienePaneles_LanzaReglaDeNegocioYNoElimina()
    {
        var nodo = FixtureAbm.NuevoNodo();
        nodo.Paneles.Add(FixtureAbm.NuevoPanel(nodo.Id));
        var nodos = new NodoRepositoryAbmFalso(nodo);
        var servicio = CrearServicio(nodos, new ParqueRepositoryAbmFalso());

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => servicio.EliminarAsync(nodo.Id));

        Assert.Contains("todavía tiene paneles asociados", error.Message);
        Assert.Empty(nodos.Eliminados);
    }
}
