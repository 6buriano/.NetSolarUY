using SolarUY.Infrastructure.Repositories;

namespace SolarUY.Infrastructure.Tests.Persistencia;

/// <summary>Pruebas de <see cref="ParqueRepository"/> contra SQLite en memoria con el modelo real de EF.</summary>
public class ParqueRepositoryTests
{
    [Fact]
    public async Task Agregar_GuardaElParque()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();

        await using (var contexto = db.NuevoContexto())
        {
            await new ParqueRepository(contexto).AgregarAsync(parque);
        }

        await using var lectura = db.NuevoContexto();
        var guardado = await new ParqueRepository(lectura).ObtenerPorIdAsync(parque.Id);
        Assert.NotNull(guardado);
        Assert.Equal("Parque Norte", guardado.Nombre);
        Assert.Equal("Salto", guardado.Ubicacion);
        Assert.Equal(12.5, guardado.CapacidadInstaladaKW, 6);
    }

    [Fact]
    public async Task ObtenerPorId_ConUnIdInexistente_DevuelveNull()
    {
        using var db = new BaseDeDatosEnMemoria();
        await using var contexto = db.NuevoContexto();

        Assert.Null(await new ParqueRepository(contexto).ObtenerPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObtenerPorId_TraeSusNodosConSusPaneles()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (parque, nodo, panel) = await Datos.SembrarAsync(db);

        await using var contexto = db.NuevoContexto();
        var cargado = await new ParqueRepository(contexto).ObtenerPorIdAsync(parque.Id);

        Assert.NotNull(cargado);
        var nodoCargado = Assert.Single(cargado.Nodos);
        Assert.Equal(nodo.Id, nodoCargado.Id);
        Assert.Equal(panel.Id, Assert.Single(nodoCargado.Paneles).Id);
    }

    [Fact]
    public async Task ObtenerTodos_DevuelveLosParquesOrdenadosPorNombre()
    {
        using var db = new BaseDeDatosEnMemoria();
        await db.GuardarAsync(Datos.NuevoParque("Zeta"), Datos.NuevoParque("Alfa"), Datos.NuevoParque("Medio"));

        await using var contexto = db.NuevoContexto();
        var parques = await new ParqueRepository(contexto).ObtenerTodosAsync();

        Assert.Equal(new[] { "Alfa", "Medio", "Zeta" }, parques.Select(parque => parque.Nombre).ToArray());
    }

    [Fact]
    public async Task ObtenerTodos_IncluyeLosNodosDeCadaParque()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (parque, nodo, _) = await Datos.SembrarAsync(db);

        await using var contexto = db.NuevoContexto();
        var parques = await new ParqueRepository(contexto).ObtenerTodosAsync();

        Assert.Equal(nodo.Id, Assert.Single(Assert.Single(parques, p => p.Id == parque.Id).Nodos).Id);
    }

    [Fact]
    public async Task Existe_DistingueUnParqueGuardadoDeUnoInexistente()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        await db.GuardarAsync(parque);

        await using var contexto = db.NuevoContexto();
        var repositorio = new ParqueRepository(contexto);

        Assert.True(await repositorio.ExisteAsync(parque.Id));
        Assert.False(await repositorio.ExisteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObtenerNombres_DevuelveElNombreDeCadaParquePorId()
    {
        using var db = new BaseDeDatosEnMemoria();
        var norte = Datos.NuevoParque("Norte");
        var sur = Datos.NuevoParque("Sur");
        await db.GuardarAsync(norte, sur);

        await using var contexto = db.NuevoContexto();
        var nombres = await new ParqueRepository(contexto).ObtenerNombresAsync();

        Assert.Equal(2, nombres.Count);
        Assert.Equal("Norte", nombres[norte.Id]);
        Assert.Equal("Sur", nombres[sur.Id]);
    }

    [Fact]
    public async Task Actualizar_GuardaLosCambiosDelParqueCargado()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        await db.GuardarAsync(parque);

        await using (var contexto = db.NuevoContexto())
        {
            var repositorio = new ParqueRepository(contexto);
            var cargado = (await repositorio.ObtenerPorIdAsync(parque.Id))!;
            cargado.Actualizar("Parque Sur", "Rivera", 30d, new DateTime(2026, 3, 1));
            await repositorio.ActualizarAsync(cargado);
        }

        await using var lectura = db.NuevoContexto();
        var guardado = (await new ParqueRepository(lectura).ObtenerPorIdAsync(parque.Id))!;
        Assert.Equal("Parque Sur", guardado.Nombre);
        Assert.Equal("Rivera", guardado.Ubicacion);
        Assert.Equal(30d, guardado.CapacidadInstaladaKW, 6);
    }

    [Fact]
    public async Task Eliminar_BorraElParqueYEnCascadaSusNodosInversoresYPaneles()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (parque, _, _) = await Datos.SembrarAsync(db);

        await using (var contexto = db.NuevoContexto())
        {
            var repositorio = new ParqueRepository(contexto);
            await repositorio.EliminarAsync((await repositorio.ObtenerPorIdAsync(parque.Id))!);
        }

        await using var lectura = db.NuevoContexto();
        Assert.Empty(lectura.ParquesSolares);
        Assert.Empty(lectura.Nodos);
        Assert.Empty(lectura.Inversores);
        Assert.Empty(lectura.Paneles);
    }
}
