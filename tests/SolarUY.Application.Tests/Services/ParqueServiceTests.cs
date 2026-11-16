using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="ParqueService"/>: consultas con sus totales, el resumen de salud y el alta,
/// modificación y baja con la regla de no borrar un parque que todavía tiene nodos.
/// </summary>
public class ParqueServiceTests
{
    private static ParqueSolar ParqueConNodoYPaneles(params (string Codigo, double Potencia, EstadoSalud Estado)[] paneles)
    {
        var parque = FixtureAbm.NuevoParque();
        var nodo = FixtureAbm.NuevoNodo(parque.Id);

        foreach (var (codigo, potencia, estado) in paneles)
        {
            nodo.Paneles.Add(FixtureAbm.NuevoPanel(nodo.Id, codigo, potencia, estado));
        }

        parque.Nodos.Add(nodo);
        return parque;
    }

    // ---------------------------------------------------------------- Consultas

    [Fact]
    public async Task ObtenerTodosAsync_DevuelveCadaParqueConSusTotales()
    {
        var parque = ParqueConNodoYPaneles(
            ("P01", 500d, EstadoSalud.OPERATIVO),
            ("P02", 250d, EstadoSalud.OPERATIVO));
        var servicio = new ParqueService(new ParqueRepositoryAbmFalso(parque, FixtureAbm.NuevoParque("Parque Sur")));

        var resultado = await servicio.ObtenerTodosAsync();

        Assert.Equal(2, resultado.Count);
        var dto = resultado[0];
        Assert.Equal(parque.Id, dto.Id);
        Assert.Equal("Parque Norte", dto.Nombre);
        Assert.Equal(1, dto.CantidadNodos);
        Assert.Equal(2, dto.CantidadPaneles);
        Assert.Equal(0.75d, dto.PotenciaNominalPanelesKW, 6);
        Assert.Equal(0, resultado[1].CantidadNodos);
        Assert.Equal(0d, resultado[1].PotenciaNominalPanelesKW, 6);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_SiExiste_DevuelveElParque()
    {
        var parque = FixtureAbm.NuevoParque();
        var servicio = new ParqueService(new ParqueRepositoryAbmFalso(parque));

        var dto = await servicio.ObtenerPorIdAsync(parque.Id);

        Assert.NotNull(dto);
        Assert.Equal(parque.Id, dto.Id);
        Assert.Equal("Salto", dto.Ubicacion);
        Assert.Equal(12.5d, dto.CapacidadInstaladaKW, 6);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_SiNoExiste_DevuelveNulo()
    {
        var servicio = new ParqueService(new ParqueRepositoryAbmFalso());

        Assert.Null(await servicio.ObtenerPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObtenerResumenAsync_CuentaLosPanelesPorEstadoDeSalud()
    {
        var parque = ParqueConNodoYPaneles(
            ("P01", 500d, EstadoSalud.OPERATIVO),
            ("P02", 500d, EstadoSalud.OPERATIVO),
            ("P03", 500d, EstadoSalud.RENDIMIENTO_BAJO),
            ("P04", 500d, EstadoSalud.FALLA_DETECTADA),
            ("P05", 500d, EstadoSalud.DESCONECTADO));
        var servicio = new ParqueService(new ParqueRepositoryAbmFalso(parque));

        var resumen = await servicio.ObtenerResumenAsync(parque.Id);

        Assert.Equal("Parque Norte", resumen.Nombre);
        Assert.Equal("Salto", resumen.Ubicacion);
        Assert.Equal(1, resumen.CantidadNodos);
        Assert.Equal(5, resumen.CantidadPaneles);
        Assert.Equal(2, resumen.PanelesOperativos);
        Assert.Equal(1, resumen.PanelesRendimientoBajo);
        Assert.Equal(1, resumen.PanelesEnFalla);
        Assert.Equal(1, resumen.PanelesDesconectados);
    }

    [Fact]
    public async Task ObtenerResumenAsync_SiElParqueNoExiste_LanzaReglaDeNegocio()
    {
        var servicio = new ParqueService(new ParqueRepositoryAbmFalso());

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.ObtenerResumenAsync(Guid.NewGuid()));

        Assert.Equal("El parque indicado no existe.", error.Message);
    }

    // ---------------------------------------------------------------- Alta, modificación y baja

    [Fact]
    public async Task CrearAsync_GuardaElParqueYDevuelveSuId()
    {
        var repositorio = new ParqueRepositoryAbmFalso();
        var servicio = new ParqueService(repositorio);

        var id = await servicio.CrearAsync(new CrearParqueRequest("  Parque Este ", "Rivera", 20d,
            new DateTime(2026, 3, 10, 18, 30, 0, DateTimeKind.Utc)));

        var guardado = Assert.Single(repositorio.Agregados);
        Assert.Equal(id, guardado.Id);
        Assert.Equal("Parque Este", guardado.Nombre);
        Assert.Equal(new DateTime(2026, 3, 10), guardado.FechaInstalacion);
    }

    [Fact]
    public async Task CrearAsync_ConDatosInvalidos_LanzaReglaDeNegocioYNoGuarda()
    {
        var repositorio = new ParqueRepositoryAbmFalso();
        var servicio = new ParqueService(repositorio);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => servicio.CrearAsync(
            new CrearParqueRequest("Parque", "Salto", 0d, FixtureAbm.Instalacion)));

        Assert.Empty(repositorio.Agregados);
    }

    [Fact]
    public async Task ActualizarAsync_SiExiste_CambiaLosDatosYLoGuarda()
    {
        var parque = FixtureAbm.NuevoParque();
        var repositorio = new ParqueRepositoryAbmFalso(parque);
        var servicio = new ParqueService(repositorio);

        var resultado = await servicio.ActualizarAsync(new ActualizarParqueRequest(parque.Id, "Parque Renovado",
            "Artigas", 30d, FixtureAbm.Instalacion));

        Assert.True(resultado);
        Assert.Same(parque, Assert.Single(repositorio.Actualizados));
        Assert.Equal("Parque Renovado", parque.Nombre);
        Assert.Equal("Artigas", parque.Ubicacion);
        Assert.Equal(30d, parque.CapacidadInstaladaKW, 6);
    }

    [Fact]
    public async Task ActualizarAsync_SiNoExiste_DevuelveFalsoYNoGuarda()
    {
        var repositorio = new ParqueRepositoryAbmFalso();
        var servicio = new ParqueService(repositorio);

        var resultado = await servicio.ActualizarAsync(new ActualizarParqueRequest(Guid.NewGuid(), "Parque",
            "Salto", 5d, FixtureAbm.Instalacion));

        Assert.False(resultado);
        Assert.Empty(repositorio.Actualizados);
    }

    [Fact]
    public async Task EliminarAsync_SiNoTieneNodos_LoElimina()
    {
        var parque = FixtureAbm.NuevoParque();
        var repositorio = new ParqueRepositoryAbmFalso(parque);
        var servicio = new ParqueService(repositorio);

        var resultado = await servicio.EliminarAsync(parque.Id);

        Assert.True(resultado);
        Assert.Same(parque, Assert.Single(repositorio.Eliminados));
    }

    [Fact]
    public async Task EliminarAsync_SiNoExiste_DevuelveFalso()
    {
        var servicio = new ParqueService(new ParqueRepositoryAbmFalso());

        Assert.False(await servicio.EliminarAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task EliminarAsync_SiTieneNodos_LanzaReglaDeNegocioYNoElimina()
    {
        var parque = ParqueConNodoYPaneles();
        var repositorio = new ParqueRepositoryAbmFalso(parque);
        var servicio = new ParqueService(repositorio);

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => servicio.EliminarAsync(parque.Id));

        Assert.Contains("todavía tiene nodos asociados", error.Message);
        Assert.Empty(repositorio.Eliminados);
    }
}
