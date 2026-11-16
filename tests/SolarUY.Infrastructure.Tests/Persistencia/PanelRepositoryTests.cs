using SolarUY.Domain.Enums;
using SolarUY.Infrastructure.Repositories;

namespace SolarUY.Infrastructure.Tests.Persistencia;

/// <summary>Pruebas de <see cref="PanelRepository"/> contra SQLite en memoria con el modelo real de EF.</summary>
public class PanelRepositoryTests
{
    [Fact]
    public async Task Agregar_GuardaElPanel()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, _) = await Datos.SembrarAsync(db);
        var panel = Datos.NuevoPanel(nodo.Id, "P02", estado: EstadoSalud.RENDIMIENTO_BAJO);

        await using (var contexto = db.NuevoContexto())
        {
            await new PanelRepository(contexto).AgregarAsync(panel);
        }

        await using var lectura = db.NuevoContexto();
        var guardado = await new PanelRepository(lectura).ObtenerPorIdAsync(panel.Id);
        Assert.NotNull(guardado);
        Assert.Equal("P02", guardado.CodigoPanel);
        Assert.Equal(EstadoSalud.RENDIMIENTO_BAJO, guardado.Estado);
        Assert.Equal(550d, guardado.PotenciaNominalW, 6);
        Assert.True(guardado.Activo);
        Assert.Equal(nodo.Id, guardado.NodoId);
    }

    [Fact]
    public async Task ObtenerPorId_ConUnIdInexistente_DevuelveNull()
    {
        using var db = new BaseDeDatosEnMemoria();
        await using var contexto = db.NuevoContexto();

        Assert.Null(await new PanelRepository(contexto).ObtenerPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObtenerTodos_OrdenaPorFabricanteYLuegoPorModelo()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, _) = await Datos.SembrarAsync(db);
        await db.GuardarAsync(
            Datos.NuevoPanel(nodo.Id, "P02", "Zeta", "Canadian"),
            Datos.NuevoPanel(nodo.Id, "P03", "Alfa", "Canadian"),
            Datos.NuevoPanel(nodo.Id, "P04", "Beta", "Astro"));

        await using var contexto = db.NuevoContexto();
        var paneles = await new PanelRepository(contexto).ObtenerTodosAsync();

        Assert.Equal("P04,P03,P02,P01", string.Join(",", paneles.Select(panel => panel.CodigoPanel)));
    }

    [Fact]
    public async Task ObtenerDescripciones_ArmaCodigoYModeloDeCadaPanel()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, _, panel) = await Datos.SembrarAsync(db);

        await using var contexto = db.NuevoContexto();
        var descripciones = await new PanelRepository(contexto).ObtenerDescripcionesAsync();

        Assert.Equal("P01 (JAM72S30)", descripciones[panel.Id]);
    }

    [Fact]
    public async Task ObtenerPorNodo_DevuelveSoloLosPanelesDeEseNodoOrdenadosPorModelo()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        var uno = Datos.NuevoNodo(parque.Id, "Uno", "SN-1");
        var dos = Datos.NuevoNodo(parque.Id, "Dos", "SN-2");
        await db.GuardarAsync(parque, uno, dos,
            Datos.NuevoPanel(uno.Id, "P01", "Zeta"),
            Datos.NuevoPanel(uno.Id, "P02", "Alfa"),
            Datos.NuevoPanel(dos.Id, "P01", "Beta"));

        await using var contexto = db.NuevoContexto();
        var paneles = await new PanelRepository(contexto).ObtenerPorNodoAsync(uno.Id);

        Assert.Equal("Alfa,Zeta", string.Join(",", paneles.Select(panel => panel.Modelo)));
    }

    [Fact]
    public async Task ObtenerPorNodo_ConEstado_FiltraPorEseEstado()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, _) = await Datos.SembrarAsync(db);
        await db.GuardarAsync(
            Datos.NuevoPanel(nodo.Id, "P02", estado: EstadoSalud.FALLA_DETECTADA),
            Datos.NuevoPanel(nodo.Id, "P03", estado: EstadoSalud.FALLA_DETECTADA));

        await using var contexto = db.NuevoContexto();
        var paneles = await new PanelRepository(contexto).ObtenerPorNodoAsync(nodo.Id, EstadoSalud.FALLA_DETECTADA);

        Assert.Equal(2, paneles.Count);
        Assert.All(paneles, panel => Assert.Equal(EstadoSalud.FALLA_DETECTADA, panel.Estado));
    }

    [Fact]
    public async Task ExisteCodigoEnNodo_SoloMiraElNodoIndicado()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, panel) = await Datos.SembrarAsync(db);

        await using var contexto = db.NuevoContexto();
        var repositorio = new PanelRepository(contexto);

        Assert.True(await repositorio.ExisteCodigoEnNodoAsync(nodo.Id, "P01"));
        Assert.True(await repositorio.ExisteCodigoEnNodoAsync(nodo.Id, "  P01 "));
        Assert.False(await repositorio.ExisteCodigoEnNodoAsync(nodo.Id, "P99"));
        Assert.False(await repositorio.ExisteCodigoEnNodoAsync(Guid.NewGuid(), "P01"));
        Assert.False(await repositorio.ExisteCodigoEnNodoAsync(nodo.Id, "P01", panel.Id));
    }

    [Fact]
    public async Task Actualizar_GuardaLosCambiosDelPanelCargado()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, panel) = await Datos.SembrarAsync(db);

        await using (var contexto = db.NuevoContexto())
        {
            var repositorio = new PanelRepository(contexto);
            var cargado = (await repositorio.ObtenerPorIdAsync(panel.Id))!;
            cargado.Actualizar("P01", "Nuevo modelo", "Otro fabricante", 600d, EstadoSalud.DESCONECTADO,
                Datos.Instalacion, nodo.Id, false);
            await repositorio.ActualizarAsync(cargado);
        }

        await using var lectura = db.NuevoContexto();
        var guardado = (await new PanelRepository(lectura).ObtenerPorIdAsync(panel.Id))!;
        Assert.Equal("Nuevo modelo", guardado.Modelo);
        Assert.Equal(600d, guardado.PotenciaNominalW, 6);
        Assert.Equal(EstadoSalud.DESCONECTADO, guardado.Estado);
        Assert.False(guardado.Activo);
    }

    [Fact]
    public async Task Eliminar_BorraElPanelYEnCascadaSusLecturasYMantenimientos()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, _, panel) = await Datos.SembrarAsync(db);
        await db.GuardarAsync(
            Datos.NuevaLectura(panel.Id, new DateTime(2026, 11, 10, 10, 0, 0)),
            Datos.NuevaTarea(panel.Id, new DateTime(2026, 11, 20)));

        await using (var contexto = db.NuevoContexto())
        {
            var repositorio = new PanelRepository(contexto);
            await repositorio.EliminarAsync((await repositorio.ObtenerPorIdAsync(panel.Id))!);
        }

        await using var lectura = db.NuevoContexto();
        Assert.Empty(lectura.Paneles);
        Assert.Empty(lectura.LecturasTelemetria);
        Assert.Empty(lectura.TareasMantenimiento);
    }
}
