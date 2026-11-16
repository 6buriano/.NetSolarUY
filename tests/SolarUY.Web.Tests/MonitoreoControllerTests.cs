using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.Services;
using SolarUY.Web.Controllers;
using SolarUY.Web.Models;

namespace SolarUY.Web.Tests;

/// <summary>Pruebas de <see cref="MonitoreoController"/>: qué parque queda elegido en la pantalla.</summary>
public class MonitoreoControllerTests
{
    private static MonitoreoController CrearControlador(params SolarUY.Domain.Entities.ParqueSolar[] parques)
        => new(new MonitoreoService(new ParqueRepositoryMonitoreoFalso(parques),
            new TelemetriaRepositoryMonitoreoFalso()));

    [Fact]
    public async Task Index_SinParques_MuestraLaFotoSinParqueElegido()
    {
        var controlador = CrearControlador();

        var resultado = await controlador.Index(null, CancellationToken.None);

        var modelo = Assert.IsType<MonitoreoIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Empty(modelo.Instantanea.Parques);
        Assert.Null(modelo.Parque);
    }

    [Fact]
    public async Task Index_SinElegirParque_MuestraElPrimeroPorNombre()
    {
        var controlador = CrearControlador(FixtureMonitoreo.Parque("Parque Sur"), FixtureMonitoreo.Parque("Parque Este"));

        var resultado = await controlador.Index(null, CancellationToken.None);

        var modelo = Assert.IsType<MonitoreoIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("Parque Este", modelo.Parque!.Nombre);
        Assert.Equal(2, modelo.Instantanea.Parques.Count);
    }

    [Fact]
    public async Task Index_ConUnParqueElegido_MuestraEseParque()
    {
        var sur = FixtureMonitoreo.Parque("Parque Sur");
        var controlador = CrearControlador(FixtureMonitoreo.Parque("Parque Este"), sur);

        var resultado = await controlador.Index(sur.Id, CancellationToken.None);

        var modelo = Assert.IsType<MonitoreoIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(sur.Id, modelo.Parque!.ParqueId);
    }

    [Fact]
    public async Task Index_ConUnParqueQueNoExiste_MuestraElPrimero()
    {
        var controlador = CrearControlador(FixtureMonitoreo.Parque("Parque Este"));

        var resultado = await controlador.Index(Guid.NewGuid(), CancellationToken.None);

        var modelo = Assert.IsType<MonitoreoIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("Parque Este", modelo.Parque!.Nombre);
    }
}
