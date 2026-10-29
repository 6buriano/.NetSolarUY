using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Tests.Entities;

public class TareaMantenimientoTests
{
    private static readonly Guid PanelId = Guid.NewGuid();
    private static readonly DateTime FechaProgramada = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

    private static TareaMantenimiento CrearTarea(
        string descripcion = "Limpieza de los paneles del sector norte",
        string? personalAsignado = "Cuadrilla 1")
        => new(PanelId, TipoMantenimiento.LIMPIEZA, FechaProgramada, descripcion, personalAsignado);

    /// <summary>
    /// Deja la tarea en el estado pedido usando las transiciones del dominio, para no repetir
    /// la secuencia en cada prueba.
    /// </summary>
    private static TareaMantenimiento CrearTareaEnEstado(EstadoMantenimiento estado)
    {
        var tarea = CrearTarea();

        switch (estado)
        {
            case EstadoMantenimiento.PENDIENTE:
                break;
            case EstadoMantenimiento.EN_PROCESO:
                tarea.Iniciar();
                break;
            case EstadoMantenimiento.COMPLETADO:
                tarea.Iniciar();
                tarea.Completar(FechaProgramada.AddHours(4));
                break;
            case EstadoMantenimiento.CANCELADO:
                tarea.Cancelar();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(estado));
        }

        return tarea;
    }

    // ---------- Constructor ----------

    [Fact]
    public void Constructor_ConDatosValidos_CreaLaTareaPendiente()
    {
        var tarea = new TareaMantenimiento(PanelId, TipoMantenimiento.INSPECCION_PREVENTIVA, FechaProgramada,
            "  Revisión de conexiones  ", "  Cuadrilla 2  ");

        Assert.NotEqual(Guid.Empty, tarea.Id);
        Assert.Equal(PanelId, tarea.PanelSolarId);
        Assert.Equal(TipoMantenimiento.INSPECCION_PREVENTIVA, tarea.Tipo);
        Assert.Equal(FechaProgramada, tarea.FechaProgramada);
        Assert.Equal("Revisión de conexiones", tarea.Descripcion);
        Assert.Equal("Cuadrilla 2", tarea.PersonalAsignado);
        Assert.Equal(EstadoMantenimiento.PENDIENTE, tarea.Estado);
        Assert.Null(tarea.FechaRealizada);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_SinPersonalAsignado_LoDejaNulo(string? personal)
        => Assert.Null(CrearTarea(personalAsignado: personal).PersonalAsignado);

    [Fact]
    public void Constructor_SinPanel_LanzaReglaDeNegocio()
        => Assert.Throws<ReglaDeNegocioException>(() =>
            new TareaMantenimiento(Guid.Empty, TipoMantenimiento.LIMPIEZA, FechaProgramada, "Limpieza", null));

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    public void Constructor_ConTipoFueraDelEnum_LanzaReglaDeNegocio(int tipo)
        => Assert.Throws<ReglaDeNegocioException>(() =>
            new TareaMantenimiento(PanelId, (TipoMantenimiento)tipo, FechaProgramada, "Limpieza", null));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_SinDescripcion_LanzaReglaDeNegocio(string descripcion)
        => Assert.Throws<ReglaDeNegocioException>(() =>
            new TareaMantenimiento(PanelId, TipoMantenimiento.LIMPIEZA, FechaProgramada, descripcion, null));

    // ---------- Iniciar ----------

    [Fact]
    public void Iniciar_DesdePendiente_PasaAEnProceso()
    {
        var tarea = CrearTarea();

        tarea.Iniciar();

        Assert.Equal(EstadoMantenimiento.EN_PROCESO, tarea.Estado);
        Assert.Null(tarea.FechaRealizada);
    }

    [Theory]
    [InlineData(EstadoMantenimiento.EN_PROCESO)]
    [InlineData(EstadoMantenimiento.COMPLETADO)]
    [InlineData(EstadoMantenimiento.CANCELADO)]
    public void Iniciar_CuandoNoEstaPendiente_LanzaReglaDeNegocio(EstadoMantenimiento estado)
        => Assert.Throws<ReglaDeNegocioException>(() => CrearTareaEnEstado(estado).Iniciar());

    // ---------- Completar ----------

    [Fact]
    public void Completar_DesdeEnProceso_CambiaElEstadoYGuardaLaFecha()
    {
        var tarea = CrearTareaEnEstado(EstadoMantenimiento.EN_PROCESO);
        var fechaRealizada = FechaProgramada.AddHours(6);

        tarea.Completar(fechaRealizada);

        Assert.Equal(EstadoMantenimiento.COMPLETADO, tarea.Estado);
        Assert.Equal(fechaRealizada, tarea.FechaRealizada);
    }

    [Theory]
    [InlineData(EstadoMantenimiento.PENDIENTE)]
    [InlineData(EstadoMantenimiento.COMPLETADO)]
    [InlineData(EstadoMantenimiento.CANCELADO)]
    public void Completar_CuandoNoEstaEnProceso_LanzaReglaDeNegocio(EstadoMantenimiento estado)
        => Assert.Throws<ReglaDeNegocioException>(() =>
            CrearTareaEnEstado(estado).Completar(FechaProgramada.AddHours(6)));

    // ---------- Cancelar ----------

    [Theory]
    [InlineData(EstadoMantenimiento.PENDIENTE)]
    [InlineData(EstadoMantenimiento.EN_PROCESO)]
    public void Cancelar_DesdePendienteOEnProceso_CancelaLaTarea(EstadoMantenimiento estado)
    {
        var tarea = CrearTareaEnEstado(estado);

        tarea.Cancelar();

        Assert.Equal(EstadoMantenimiento.CANCELADO, tarea.Estado);
        Assert.Null(tarea.FechaRealizada);
    }

    [Fact]
    public void Cancelar_UnaTareaCompletada_LanzaReglaDeNegocio()
    {
        var tarea = CrearTareaEnEstado(EstadoMantenimiento.COMPLETADO);
        var fechaRealizada = tarea.FechaRealizada;

        Assert.Throws<ReglaDeNegocioException>(() => tarea.Cancelar());
        Assert.Equal(EstadoMantenimiento.COMPLETADO, tarea.Estado);
        Assert.Equal(fechaRealizada, tarea.FechaRealizada);
    }

    [Fact]
    public void Cancelar_UnaTareaYaCancelada_LaDejaComoEstaba()
    {
        // El dominio sólo impide cancelar una tarea completada: cancelar dos veces no falla.
        var tarea = CrearTareaEnEstado(EstadoMantenimiento.CANCELADO);

        tarea.Cancelar();

        Assert.Equal(EstadoMantenimiento.CANCELADO, tarea.Estado);
    }

    // ---------- Reprogramar ----------

    [Fact]
    public void Reprogramar_CambiaLaFechaLaDescripcionYElPersonal()
    {
        var tarea = CrearTarea();
        var nuevaFecha = FechaProgramada.AddDays(7);

        tarea.Reprogramar(nuevaFecha, "  Reemplazo de dos paneles  ", "  Cuadrilla 3  ");

        Assert.Equal(nuevaFecha, tarea.FechaProgramada);
        Assert.Equal("Reemplazo de dos paneles", tarea.Descripcion);
        Assert.Equal("Cuadrilla 3", tarea.PersonalAsignado);
        Assert.Equal(EstadoMantenimiento.PENDIENTE, tarea.Estado);
    }

    [Fact]
    public void Reprogramar_ConPersonalVacio_LoDejaNulo()
    {
        var tarea = CrearTarea();

        tarea.Reprogramar(FechaProgramada.AddDays(1), "Revisión", "   ");

        Assert.Null(tarea.PersonalAsignado);
    }

    [Fact]
    public void Reprogramar_UnaTareaCompletada_LanzaReglaDeNegocio()
    {
        var tarea = CrearTareaEnEstado(EstadoMantenimiento.COMPLETADO);

        Assert.Throws<ReglaDeNegocioException>(() =>
            tarea.Reprogramar(FechaProgramada.AddDays(7), "Otra fecha", null));

        Assert.Equal(FechaProgramada, tarea.FechaProgramada);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reprogramar_SinDescripcion_LanzaReglaDeNegocio(string descripcion)
    {
        var tarea = CrearTarea();

        Assert.Throws<ReglaDeNegocioException>(() =>
            tarea.Reprogramar(FechaProgramada.AddDays(1), descripcion, null));

        Assert.Equal(FechaProgramada, tarea.FechaProgramada);
    }
}
