using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="MantenimientoService"/>: que consulte, cree y cambie de estado las tareas a
/// través de los repositorios y que respete las reglas del dominio (por ejemplo, no tocar una tarea
/// ya completada). Los repositorios son falsos y llevan la cuenta de lo que se les pidió.
/// </summary>
public class MantenimientoServiceTests
{
    private static readonly DateTime Ahora = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaProgramada = new(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc);

    private static TareaMantenimiento NuevaTarea(Guid? panelId = null, string descripcion = "Limpieza general")
        => new(panelId ?? Guid.NewGuid(), TipoMantenimiento.LIMPIEZA, FechaProgramada, descripcion, "Ana");

    private static TareaMantenimiento TareaCompletada()
    {
        var tarea = NuevaTarea();
        tarea.Iniciar();
        tarea.Completar(Ahora.AddHours(-2), "Se limpió el panel", Ahora);
        return tarea;
    }

    private static MantenimientoService CrearServicio(MantenimientoRepositoryFalso tareas,
        PanelRepositoryFalso? paneles = null)
        => new(tareas, paneles ?? new PanelRepositoryFalso(), new RelojFijo(new DateTimeOffset(Ahora)));

    // ---------------------------------------------------------------- Consultas

    [Fact]
    public async Task ObtenerTodosAsync_IncluyeLaDescripcionDelPanelEnCadaTarea()
    {
        var tarea = NuevaTarea();
        var tareas = new MantenimientoRepositoryFalso(tarea);
        var paneles = new PanelRepositoryFalso
        {
            Descripciones = { [tarea.PanelSolarId] = "P01 (CS6P-300)" }
        };

        var resultado = await CrearServicio(tareas, paneles).ObtenerTodosAsync();

        var dto = Assert.Single(resultado);
        Assert.Equal(tarea.Id, dto.Id);
        Assert.Equal("P01 (CS6P-300)", dto.PanelDescripcion);
    }

    [Fact]
    public async Task ObtenerTodosAsync_SiElPanelNoTieneDescripcion_LaDejaVacia()
    {
        var tareas = new MantenimientoRepositoryFalso(NuevaTarea());

        var resultado = await CrearServicio(tareas).ObtenerTodosAsync();

        Assert.Null(Assert.Single(resultado).PanelDescripcion);
    }

    [Fact]
    public async Task ObtenerTodosAsync_LeAvisaAlRepositorioQueEstadoSeFiltra()
    {
        var tareas = new MantenimientoRepositoryFalso(NuevaTarea());

        await CrearServicio(tareas).ObtenerTodosAsync(EstadoMantenimiento.EN_PROCESO);

        Assert.Equal(EstadoMantenimiento.EN_PROCESO, tareas.UltimoEstadoPedido);
    }

    [Fact]
    public async Task ObtenerPorPanelAsync_DevuelveSoloLasTareasDeEsePanel()
    {
        var delPanel = NuevaTarea();
        var deOtroPanel = NuevaTarea();
        var tareas = new MantenimientoRepositoryFalso(delPanel, deOtroPanel);

        var resultado = await CrearServicio(tareas).ObtenerPorPanelAsync(delPanel.PanelSolarId);

        Assert.Equal(delPanel.Id, Assert.Single(resultado).Id);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_SiLaTareaNoExiste_DevuelveNull()
    {
        var resultado = await CrearServicio(new MantenimientoRepositoryFalso()).ObtenerPorIdAsync(Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_SiLaTareaExiste_LaDevuelveConLaDescripcionDelPanel()
    {
        var tarea = NuevaTarea();
        var paneles = new PanelRepositoryFalso
        {
            Descripciones = { [tarea.PanelSolarId] = "P02 (CS6P-300)" }
        };

        var dto = await CrearServicio(new MantenimientoRepositoryFalso(tarea), paneles).ObtenerPorIdAsync(tarea.Id);

        Assert.NotNull(dto);
        Assert.Equal(tarea.Id, dto.Id);
        Assert.Equal("P02 (CS6P-300)", dto.PanelDescripcion);
    }

    // ---------------------------------------------------------------- Crear

    [Fact]
    public async Task CrearAsync_SiElPanelNoExiste_LanzaExcepcionYNoGuardaNada()
    {
        var tareas = new MantenimientoRepositoryFalso();
        var servicio = CrearServicio(tareas, new PanelRepositoryFalso());
        var pedido = new CrearMantenimientoRequest(Guid.NewGuid(), TipoMantenimiento.LIMPIEZA, FechaProgramada,
            "Limpieza general", "Ana");

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => servicio.CrearAsync(pedido));

        Assert.Equal("El panel seleccionado no existe.", error.Message);
        Assert.Empty(tareas.Agregadas);
    }

    [Fact]
    public async Task CrearAsync_SiElPanelExiste_GuardaUnaTareaPendienteYDevuelveSuId()
    {
        var panel = PanelDePrueba();
        var tareas = new MantenimientoRepositoryFalso();
        var servicio = CrearServicio(tareas, new PanelRepositoryFalso { Panel = panel });
        var pedido = new CrearMantenimientoRequest(panel.Id, TipoMantenimiento.REPARACION, FechaProgramada,
            "  Cambiar conector  ", "Luis");

        var id = await servicio.CrearAsync(pedido);

        var guardada = Assert.Single(tareas.Agregadas);
        Assert.Equal(id, guardada.Id);
        Assert.Equal(panel.Id, guardada.PanelSolarId);
        Assert.Equal(EstadoMantenimiento.PENDIENTE, guardada.Estado);
        Assert.Equal("Cambiar conector", guardada.Descripcion);
    }

    // ---------------------------------------------------------------- Reprogramar

    [Fact]
    public async Task ReprogramarAsync_SiLaTareaNoExiste_DevuelveFalseYNoActualiza()
    {
        var tareas = new MantenimientoRepositoryFalso();
        var pedido = new ReprogramarMantenimientoRequest(Guid.NewGuid(), FechaProgramada, "Nueva", null);

        var resultado = await CrearServicio(tareas).ReprogramarAsync(pedido);

        Assert.False(resultado);
        Assert.Equal(0, tareas.Actualizaciones);
    }

    [Fact]
    public async Task ReprogramarAsync_SiLaTareaExiste_CambiaLosDatosYActualiza()
    {
        var tarea = NuevaTarea();
        var tareas = new MantenimientoRepositoryFalso(tarea);
        var nuevaFecha = FechaProgramada.AddDays(7);
        var pedido = new ReprogramarMantenimientoRequest(tarea.Id, nuevaFecha, "Inspección completa", "Marta");

        var resultado = await CrearServicio(tareas).ReprogramarAsync(pedido);

        Assert.True(resultado);
        Assert.Equal(nuevaFecha, tarea.FechaProgramada);
        Assert.Equal("Inspección completa", tarea.Descripcion);
        Assert.Equal("Marta", tarea.PersonalAsignado);
        Assert.Equal(1, tareas.Actualizaciones);
    }

    [Fact]
    public async Task ReprogramarAsync_SiLaTareaYaSeCompleto_LanzaExcepcionYNoActualiza()
    {
        var tarea = TareaCompletada();
        var tareas = new MantenimientoRepositoryFalso(tarea);
        var pedido = new ReprogramarMantenimientoRequest(tarea.Id, FechaProgramada, "Otra vez", null);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => CrearServicio(tareas).ReprogramarAsync(pedido));

        Assert.Equal(0, tareas.Actualizaciones);
    }

    // ---------------------------------------------------------------- Cambios de estado

    [Fact]
    public async Task IniciarAsync_SiLaTareaExiste_PasaAEnProcesoYActualiza()
    {
        var tarea = NuevaTarea();
        var tareas = new MantenimientoRepositoryFalso(tarea);

        var resultado = await CrearServicio(tareas).IniciarAsync(tarea.Id);

        Assert.True(resultado);
        Assert.Equal(EstadoMantenimiento.EN_PROCESO, tarea.Estado);
        Assert.Equal(1, tareas.Actualizaciones);
    }

    [Fact]
    public async Task IniciarAsync_SiLaTareaNoExiste_DevuelveFalseYNoActualiza()
    {
        var tareas = new MantenimientoRepositoryFalso();

        var resultado = await CrearServicio(tareas).IniciarAsync(Guid.NewGuid());

        Assert.False(resultado);
        Assert.Equal(0, tareas.Actualizaciones);
    }

    [Fact]
    public async Task IniciarAsync_SiLaTareaYaEstabaEnProceso_LanzaExcepcionYNoActualiza()
    {
        var tarea = NuevaTarea();
        tarea.Iniciar();
        var tareas = new MantenimientoRepositoryFalso(tarea);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => CrearServicio(tareas).IniciarAsync(tarea.Id));

        Assert.Equal(0, tareas.Actualizaciones);
    }

    [Fact]
    public async Task CompletarAsync_ConFechaValida_CierraLaTareaConLasObservaciones()
    {
        var tarea = NuevaTarea();
        tarea.Iniciar();
        var tareas = new MantenimientoRepositoryFalso(tarea);
        var fechaRealizada = Ahora.AddHours(-3);

        var resultado = await CrearServicio(tareas).CompletarAsync(tarea.Id, fechaRealizada, "  Todo en orden  ");

        Assert.True(resultado);
        Assert.Equal(EstadoMantenimiento.COMPLETADO, tarea.Estado);
        Assert.Equal(fechaRealizada, tarea.FechaRealizada);
        Assert.Equal("Todo en orden", tarea.ObservacionesCierre);
        Assert.Equal(1, tareas.Actualizaciones);
    }

    [Fact]
    public async Task CompletarAsync_ConFechaPosteriorAlRelojDelServicio_LanzaExcepcionYNoActualiza()
    {
        var tarea = NuevaTarea();
        tarea.Iniciar();
        var tareas = new MantenimientoRepositoryFalso(tarea);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => CrearServicio(tareas).CompletarAsync(tarea.Id, Ahora.AddMinutes(1), null));

        Assert.Equal(EstadoMantenimiento.EN_PROCESO, tarea.Estado);
        Assert.Equal(0, tareas.Actualizaciones);
    }

    [Fact]
    public async Task CompletarAsync_SiLaTareaNoExiste_DevuelveFalse()
    {
        var resultado = await CrearServicio(new MantenimientoRepositoryFalso())
            .CompletarAsync(Guid.NewGuid(), Ahora.AddHours(-1), null);

        Assert.False(resultado);
    }

    [Fact]
    public async Task CancelarAsync_SiLaTareaExiste_LaCancelaYActualiza()
    {
        var tarea = NuevaTarea();
        var tareas = new MantenimientoRepositoryFalso(tarea);

        var resultado = await CrearServicio(tareas).CancelarAsync(tarea.Id);

        Assert.True(resultado);
        Assert.Equal(EstadoMantenimiento.CANCELADO, tarea.Estado);
        Assert.Equal(1, tareas.Actualizaciones);
    }

    [Fact]
    public async Task CancelarAsync_SiLaTareaNoExiste_DevuelveFalse()
    {
        var resultado = await CrearServicio(new MantenimientoRepositoryFalso()).CancelarAsync(Guid.NewGuid());

        Assert.False(resultado);
    }

    [Fact]
    public async Task CancelarAsync_SiLaTareaYaSeCompleto_LanzaExcepcionYNoActualiza()
    {
        var tarea = TareaCompletada();
        var tareas = new MantenimientoRepositoryFalso(tarea);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => CrearServicio(tareas).CancelarAsync(tarea.Id));

        Assert.Equal(EstadoMantenimiento.COMPLETADO, tarea.Estado);
        Assert.Equal(0, tareas.Actualizaciones);
    }

    // ---------------------------------------------------------------- Apoyo

    private static PanelSolar PanelDePrueba()
    {
        var parque = FixtureMonitoreo.Parque();
        var nodo = FixtureMonitoreo.Nodo(parque);
        return FixtureMonitoreo.Panel(nodo, "P01");
    }

    /// <summary>Reloj congelado: la validación de la fecha de realización no depende de la hora de la máquina.</summary>
    private sealed class RelojFijo(DateTimeOffset momento) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => momento;
    }

    /// <summary>Repositorio en memoria que cuenta cuántas veces se guardó o actualizó algo.</summary>
    private sealed class MantenimientoRepositoryFalso(params TareaMantenimiento[] tareasIniciales)
        : IMantenimientoRepository
    {
        private readonly List<TareaMantenimiento> _tareas = [.. tareasIniciales];

        public List<TareaMantenimiento> Agregadas { get; } = [];
        public int Actualizaciones { get; private set; }
        public EstadoMantenimiento? UltimoEstadoPedido { get; private set; }

        public Task<IReadOnlyList<TareaMantenimiento>> ObtenerTodosAsync(EstadoMantenimiento? estado = null,
            CancellationToken cancellationToken = default)
        {
            UltimoEstadoPedido = estado;
            return Task.FromResult<IReadOnlyList<TareaMantenimiento>>(_tareas.ToList());
        }

        public Task<IReadOnlyList<TareaMantenimiento>> ObtenerPorPanelAsync(Guid panelSolarId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TareaMantenimiento>>(
                _tareas.Where(tarea => tarea.PanelSolarId == panelSolarId).ToList());

        public Task<TareaMantenimiento?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_tareas.FirstOrDefault(tarea => tarea.Id == id));

        public Task AgregarAsync(TareaMantenimiento tarea, CancellationToken cancellationToken = default)
        {
            Agregadas.Add(tarea);
            _tareas.Add(tarea);
            return Task.CompletedTask;
        }

        public Task ActualizarAsync(TareaMantenimiento tarea, CancellationToken cancellationToken = default)
        {
            Actualizaciones++;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Repositorio de paneles falso: solo importan la búsqueda por id y las descripciones. El resto
    /// falla si alguien lo llama, para detectar usos que la prueba no esperaba.
    /// </summary>
    private sealed class PanelRepositoryFalso : IPanelRepository
    {
        public PanelSolar? Panel { get; init; }
        public Dictionary<Guid, string> Descripciones { get; } = [];

        public Task<PanelSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Panel is not null && Panel.Id == id ? Panel : null);

        public Task<IReadOnlyDictionary<Guid, string>> ObtenerDescripcionesAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, string>>(Descripciones);

        public Task<IReadOnlyList<PanelSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<PanelSolar>> ObtenerPorNodoAsync(Guid nodoId, EstadoSalud? estado = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> ExisteCodigoEnNodoAsync(Guid nodoId, string codigoPanel, Guid? panelExcluidoId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AgregarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ActualizarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task EliminarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
