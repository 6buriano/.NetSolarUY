using SolarUY.Domain.Entities;
using SolarUY.Infrastructure.Repositories;

namespace SolarUY.Infrastructure.Tests.Persistencia;

/// <summary>Pruebas de <see cref="NodoRepository"/> contra SQLite en memoria con el modelo real de EF.</summary>
public class NodoRepositoryTests
{
    [Fact]
    public async Task Agregar_GuardaElNodoConSuInversorYSuSensor()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        await db.GuardarAsync(parque);
        var nodo = Datos.NuevoNodo(parque.Id, hardware: "ARM-001");

        await using (var contexto = db.NuevoContexto())
        {
            await new NodoRepository(contexto).AgregarAsync(nodo);
        }

        await using var lectura = db.NuevoContexto();
        var guardado = await new NodoRepository(lectura).ObtenerPorIdAsync(nodo.Id);
        Assert.NotNull(guardado);
        Assert.Equal("Nodo A", guardado.Nombre);
        Assert.Equal("SN-001", guardado.Inversor.NumeroSerie);
        Assert.Equal("ARM-001", guardado.Inversor.Sensor!.IdentificadorHardwareARM);
        Assert.Equal("http://sensor-a:5100", guardado.Inversor.Sensor.UrlBase);
        Assert.Empty(guardado.Paneles);
    }

    [Fact]
    public async Task ObtenerPorId_ConUnIdInexistente_DevuelveNull()
    {
        using var db = new BaseDeDatosEnMemoria();
        await using var contexto = db.NuevoContexto();

        Assert.Null(await new NodoRepository(contexto).ObtenerPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObtenerPorId_TraeLosPanelesDelNodo()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, panel) = await Datos.SembrarAsync(db);

        await using var contexto = db.NuevoContexto();
        var cargado = await new NodoRepository(contexto).ObtenerPorIdAsync(nodo.Id);

        Assert.Equal(panel.Id, Assert.Single(cargado!.Paneles).Id);
    }

    [Fact]
    public async Task ObtenerTodos_DevuelveLosNodosOrdenadosPorNombreConSuInversorYSensor()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        await db.GuardarAsync(parque,
            Datos.NuevoNodo(parque.Id, "Zeta", "SN-1", "ARM-1"),
            Datos.NuevoNodo(parque.Id, "Alfa", "SN-2"));

        await using var contexto = db.NuevoContexto();
        var nodos = await new NodoRepository(contexto).ObtenerTodosAsync();

        Assert.Equal(new[] { "Alfa", "Zeta" }, nodos.Select(nodo => nodo.Nombre).ToArray());
        Assert.Null(nodos[0].Inversor.Sensor);
        Assert.Equal("ARM-1", nodos[1].Inversor.Sensor!.IdentificadorHardwareARM);
    }

    [Fact]
    public async Task ObtenerPorParque_DevuelveSoloLosNodosDeEseParque()
    {
        using var db = new BaseDeDatosEnMemoria();
        var norte = Datos.NuevoParque("Norte");
        var sur = Datos.NuevoParque("Sur");
        await db.GuardarAsync(norte, sur,
            Datos.NuevoNodo(norte.Id, "Del norte", "SN-1"),
            Datos.NuevoNodo(sur.Id, "Del sur", "SN-2"));

        await using var contexto = db.NuevoContexto();
        var nodos = await new NodoRepository(contexto).ObtenerPorParqueAsync(sur.Id);

        Assert.Equal("Del sur", Assert.Single(nodos).Nombre);
    }

    [Fact]
    public async Task Existe_DistingueUnNodoGuardadoDeUnoInexistente()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, _) = await Datos.SembrarAsync(db);

        await using var contexto = db.NuevoContexto();
        var repositorio = new NodoRepository(contexto);

        Assert.True(await repositorio.ExisteAsync(nodo.Id));
        Assert.False(await repositorio.ExisteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObtenerNombres_DevuelveElNombreDeCadaNodoPorId()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        var uno = Datos.NuevoNodo(parque.Id, "Uno", "SN-1");
        var dos = Datos.NuevoNodo(parque.Id, "Dos", "SN-2");
        await db.GuardarAsync(parque, uno, dos);

        await using var contexto = db.NuevoContexto();
        var nombres = await new NodoRepository(contexto).ObtenerNombresAsync();

        Assert.Equal("Uno", nombres[uno.Id]);
        Assert.Equal("Dos", nombres[dos.Id]);
    }

    // ---------------------------------------------------------------- Duplicados

    [Fact]
    public async Task ExisteNumeroSerieInversor_DetectaUnNumeroYaRegistrado()
    {
        using var db = new BaseDeDatosEnMemoria();
        await Datos.SembrarAsync(db);

        await using var contexto = db.NuevoContexto();
        var repositorio = new NodoRepository(contexto);

        Assert.True(await repositorio.ExisteNumeroSerieInversorAsync("SN-001"));
        Assert.True(await repositorio.ExisteNumeroSerieInversorAsync("  SN-001  "));
        Assert.False(await repositorio.ExisteNumeroSerieInversorAsync("SN-999"));
    }

    [Fact]
    public async Task ExisteNumeroSerieInversor_NoCuentaElDelPropioNodoQueSeEdita()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, _) = await Datos.SembrarAsync(db);

        await using var contexto = db.NuevoContexto();
        var repositorio = new NodoRepository(contexto);

        Assert.False(await repositorio.ExisteNumeroSerieInversorAsync("SN-001", nodo.Id));
        Assert.True(await repositorio.ExisteNumeroSerieInversorAsync("SN-001", Guid.NewGuid()));
    }

    [Fact]
    public async Task ExisteIdentificadorHardware_DetectaUnIdentificadorYaRegistrado()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        await db.GuardarAsync(parque, Datos.NuevoNodo(parque.Id, hardware: "ARM-001"));

        await using var contexto = db.NuevoContexto();
        var repositorio = new NodoRepository(contexto);

        Assert.True(await repositorio.ExisteIdentificadorHardwareAsync("ARM-001"));
        Assert.True(await repositorio.ExisteIdentificadorHardwareAsync(" ARM-001 "));
        Assert.False(await repositorio.ExisteIdentificadorHardwareAsync("ARM-002"));
    }

    [Fact]
    public async Task ExisteIdentificadorHardware_NoCuentaElSensorDelPropioNodoQueSeEdita()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        var nodo = Datos.NuevoNodo(parque.Id, hardware: "ARM-001");
        await db.GuardarAsync(parque, nodo);

        await using var contexto = db.NuevoContexto();
        var repositorio = new NodoRepository(contexto);

        Assert.False(await repositorio.ExisteIdentificadorHardwareAsync("ARM-001", nodo.Id));
        Assert.True(await repositorio.ExisteIdentificadorHardwareAsync("ARM-001", Guid.NewGuid()));
    }

    // ---------------------------------------------------------------- Sensores activos

    [Fact]
    public async Task ObtenerConSensoresActivos_DejaAfueraLosNodosSinSensorOConSensorInactivo()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        var activo = Datos.NuevoNodo(parque.Id, "Activo", "SN-1", "ARM-1");
        var inactivo = Datos.NuevoNodo(parque.Id, "Inactivo", "SN-2", "ARM-2", sensorActivo: false);
        var sinSensor = Datos.NuevoNodo(parque.Id, "SinSensor", "SN-3");
        await db.GuardarAsync(parque, activo, inactivo, sinSensor, Datos.NuevoPanel(activo.Id));

        await using var contexto = db.NuevoContexto();
        var nodos = await new NodoRepository(contexto).ObtenerConSensoresActivosAsync();

        var nodo = Assert.Single(nodos);
        Assert.Equal("Activo", nodo.Nombre);
        Assert.Equal("ARM-1", nodo.Inversor.Sensor!.IdentificadorHardwareARM);
        Assert.Single(nodo.Paneles);
    }

    // ---------------------------------------------------------------- Escritura

    [Fact]
    public async Task Actualizar_GuardaLosCambiosDelNodoCargado()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, _) = await Datos.SembrarAsync(db);

        await using (var contexto = db.NuevoContexto())
        {
            var repositorio = new NodoRepository(contexto);
            var cargado = (await repositorio.ObtenerPorIdAsync(nodo.Id))!;
            cargado.Actualizar("Nodo Renombrado", "Sector 9", cargado.ParqueSolarId);
            cargado.Inversor.Actualizar("Otro modelo", "SN-777", 90d, false);
            await repositorio.ActualizarAsync(cargado);
        }

        await using var lectura = db.NuevoContexto();
        var guardado = (await new NodoRepository(lectura).ObtenerPorIdAsync(nodo.Id))!;
        Assert.Equal("Nodo Renombrado", guardado.Nombre);
        Assert.Equal("Sector 9", guardado.UbicacionDentroParque);
        Assert.Equal("SN-777", guardado.Inversor.NumeroSerie);
        Assert.False(guardado.Inversor.VolcadoRedActivo);
    }

    [Fact]
    public async Task Actualizar_ConUnSensorNuevo_LoInsertaEnLugarDeActualizarlo()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, _) = await Datos.SembrarAsync(db);

        await using (var contexto = db.NuevoContexto())
        {
            var repositorio = new NodoRepository(contexto);
            var cargado = (await repositorio.ObtenerPorIdAsync(nodo.Id))!;
            var sensor = new SensorDispositivo("ARM-NUEVO", "2.0", true, "http://sensor-b:5100");
            cargado.Inversor.RegistrarSensor(sensor);
            await repositorio.ActualizarAsync(cargado, sensor);
        }

        await using var lectura = db.NuevoContexto();
        var guardado = (await new NodoRepository(lectura).ObtenerPorIdAsync(nodo.Id))!;
        Assert.Equal("ARM-NUEVO", guardado.Inversor.Sensor!.IdentificadorHardwareARM);
        Assert.Equal(guardado.Inversor.Id, guardado.Inversor.Sensor.InversorId);
    }

    [Fact]
    public async Task Eliminar_BorraElNodoYEnCascadaSuInversorSuSensorYSusPaneles()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        var nodo = Datos.NuevoNodo(parque.Id, hardware: "ARM-001");
        await db.GuardarAsync(parque, nodo, Datos.NuevoPanel(nodo.Id));

        await using (var contexto = db.NuevoContexto())
        {
            var repositorio = new NodoRepository(contexto);
            await repositorio.EliminarAsync((await repositorio.ObtenerPorIdAsync(nodo.Id))!);
        }

        await using var lectura = db.NuevoContexto();
        Assert.Empty(lectura.Nodos);
        Assert.Empty(lectura.Inversores);
        Assert.Empty(lectura.SensoresDispositivos);
        Assert.Empty(lectura.Paneles);
        Assert.Single(lectura.ParquesSolares);
    }
}
