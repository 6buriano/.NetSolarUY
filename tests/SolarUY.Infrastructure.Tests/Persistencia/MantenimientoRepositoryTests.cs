using SolarUY.Domain.Enums;
using SolarUY.Infrastructure.Repositories;

namespace SolarUY.Infrastructure.Tests.Persistencia;

/// <summary>Pruebas de <see cref="MantenimientoRepository"/> contra SQLite en memoria con el modelo real de EF.</summary>
public class MantenimientoRepositoryTests
{
    private static readonly DateTime Diez = new(2026, 11, 10);
    private static readonly DateTime Quince = new(2026, 11, 15);
    private static readonly DateTime Veinte = new(2026, 11, 20);

    [Fact]
    public async Task Agregar_GuardaLaTareaPendiente()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, _, panel) = await Datos.SembrarAsync(db);
        var tarea = Datos.NuevaTarea(panel.Id, Quince, TipoMantenimiento.REPARACION, "Cambiar conector");

        await using (var contexto = db.NuevoContexto())
        {
            await new MantenimientoRepository(contexto).AgregarAsync(tarea);
        }

        await using var lectura = db.NuevoContexto();
        var guardada = await new MantenimientoRepository(lectura).ObtenerPorIdAsync(tarea.Id);
        Assert.NotNull(guardada);
        Assert.Equal(TipoMantenimiento.REPARACION, guardada.Tipo);
        Assert.Equal(EstadoMantenimiento.PENDIENTE, guardada.Estado);
        Assert.Equal("Cambiar conector", guardada.Descripcion);
        Assert.Equal("Ana", guardada.PersonalAsignado);
        Assert.Equal(Quince, guardada.FechaProgramada);
        Assert.Null(guardada.FechaRealizada);
    }

    [Fact]
    public async Task ObtenerPorId_ConUnIdInexistente_DevuelveNull()
    {
        using var db = new BaseDeDatosEnMemoria();
        await using var contexto = db.NuevoContexto();

        Assert.Null(await new MantenimientoRepository(contexto).ObtenerPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObtenerTodos_DevuelveLasTareasPorFechaProgramadaCreciente()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, _, panel) = await Datos.SembrarAsync(db);
        await db.GuardarAsync(
            Datos.NuevaTarea(panel.Id, Veinte, descripcion: "tercera"),
            Datos.NuevaTarea(panel.Id, Diez, descripcion: "primera"),
            Datos.NuevaTarea(panel.Id, Quince, descripcion: "segunda"));

        await using var contexto = db.NuevoContexto();
        var tareas = await new MantenimientoRepository(contexto).ObtenerTodosAsync();

        Assert.Equal(new[] { "primera", "segunda", "tercera" }, tareas.Select(tarea => tarea.Descripcion).ToArray());
    }

    [Fact]
    public async Task ObtenerTodos_ConEstado_FiltraPorEseEstado()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, _, panel) = await Datos.SembrarAsync(db);
        var enProceso = Datos.NuevaTarea(panel.Id, Diez, descripcion: "en proceso");
        enProceso.Iniciar();
        await db.GuardarAsync(enProceso, Datos.NuevaTarea(panel.Id, Quince, descripcion: "pendiente"));

        await using var contexto = db.NuevoContexto();
        var tareas = await new MantenimientoRepository(contexto).ObtenerTodosAsync(EstadoMantenimiento.EN_PROCESO);

        Assert.Equal("en proceso", Assert.Single(tareas).Descripcion);
    }

    [Fact]
    public async Task ObtenerPorPanel_DevuelveSoloLasDeEsePanelDeLaMasNuevaALaMasVieja()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, panel) = await Datos.SembrarAsync(db);
        var otro = Datos.NuevoPanel(nodo.Id, "P02");
        await db.GuardarAsync(otro,
            Datos.NuevaTarea(panel.Id, Diez, descripcion: "vieja"),
            Datos.NuevaTarea(panel.Id, Veinte, descripcion: "nueva"),
            Datos.NuevaTarea(otro.Id, Quince, descripcion: "ajena"));

        await using var contexto = db.NuevoContexto();
        var tareas = await new MantenimientoRepository(contexto).ObtenerPorPanelAsync(panel.Id);

        Assert.Equal(new[] { "nueva", "vieja" }, tareas.Select(tarea => tarea.Descripcion).ToArray());
    }

    [Fact]
    public async Task Actualizar_GuardaElCambioDeEstadoDeLaTareaCargada()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, _, panel) = await Datos.SembrarAsync(db);
        var tarea = Datos.NuevaTarea(panel.Id, Quince);
        await db.GuardarAsync(tarea);

        await using (var contexto = db.NuevoContexto())
        {
            var repositorio = new MantenimientoRepository(contexto);
            var cargada = (await repositorio.ObtenerPorIdAsync(tarea.Id))!;
            cargada.Iniciar();
            cargada.Completar(Quince, "Todo en orden", Veinte);
            await repositorio.ActualizarAsync(cargada);
        }

        await using var lectura = db.NuevoContexto();
        var guardada = (await new MantenimientoRepository(lectura).ObtenerPorIdAsync(tarea.Id))!;
        Assert.Equal(EstadoMantenimiento.COMPLETADO, guardada.Estado);
        Assert.Equal(Quince, guardada.FechaRealizada);
        Assert.Equal("Todo en orden", guardada.ObservacionesCierre);
    }
}
