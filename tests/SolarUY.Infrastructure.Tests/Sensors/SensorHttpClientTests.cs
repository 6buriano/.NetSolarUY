using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.Application.Sensors;
using SolarUY.Infrastructure.Sensors;

namespace SolarUY.Infrastructure.Tests.Sensors;

/// <summary>
/// Pruebas de <see cref="SensorHttpClient"/>: lo que se le pide al sensor, cómo traduce la respuesta
/// y que ninguna falla de la red le llegue al resto del sistema como excepción.
/// </summary>
public class SensorHttpClientTests
{
    private const string Url = "http://sensor-a:5100/";

    private const string EstadoJson = """
        {
          "identificadorHardwareARM": "ARM-001",
          "fechaHoraUtc": "2026-11-12T15:30:00",
          "volcadoRedActivo": true,
          "eficienciaConversion": 0.97,
          "paneles": [
            { "codigoPanel": "P01", "activo": true, "corrienteContinua": 8.5, "voltaje": 40.2,
              "potenciaGeneradaWatts": 341.7, "temperaturaCelsius": 41.5 },
            { "codigoPanel": "P02", "activo": false, "corrienteContinua": 0, "voltaje": 0,
              "potenciaGeneradaWatts": 0, "temperaturaCelsius": 25 }
          ]
        }
        """;

    private static SensorHttpClient Crear(ManejadorHttpFalso manejador)
        => new(new HttpClient(manejador), NullLogger<SensorHttpClient>.Instance);

    // ---------------------------------------------------------------- Estado

    [Fact]
    public async Task ObtenerEstado_ConRespuestaValida_TraduceElEstadoDelSensor()
    {
        var manejador = ManejadorHttpFalso.Responde(HttpStatusCode.OK, EstadoJson);

        var estado = await Crear(manejador).ObtenerEstadoAsync(Url, "ARM-001");

        Assert.NotNull(estado);
        Assert.Equal("ARM-001", estado.IdentificadorHardwareARM);
        Assert.Equal(DateTimeKind.Utc, estado.FechaHoraUtc.Kind);
        Assert.Equal(new DateTime(2026, 11, 12, 15, 30, 0), estado.FechaHoraUtc);
        Assert.True(estado.VolcadoRedActivo);
        // El sensor informa 0,97 y el dominio trabaja en porcentaje.
        Assert.Equal(97d, estado.EficienciaConversion, 6);
        Assert.Equal(2, estado.Paneles.Count);
        Assert.Equal("P01", estado.Paneles[0].CodigoPanel);
        Assert.Equal(341.7, estado.Paneles[0].PotenciaGeneradaWatts, 6);
        Assert.False(estado.Paneles[1].Activo);
        Assert.Equal("GET http://sensor-a:5100/sensores/ARM-001/estado", Assert.Single(manejador.Pedidos));
    }

    [Fact]
    public async Task ObtenerEstado_EscapaElIdentificadorEnLaUrl()
    {
        var manejador = ManejadorHttpFalso.Responde(HttpStatusCode.NotFound);

        await Crear(manejador).ObtenerEstadoAsync(Url, "ARM 001/x");

        Assert.Equal("GET http://sensor-a:5100/sensores/ARM%20001%2Fx/estado", Assert.Single(manejador.Pedidos));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ObtenerEstado_ConRespuestaDeError_DevuelveNull(HttpStatusCode codigo)
    {
        var estado = await Crear(ManejadorHttpFalso.Responde(codigo)).ObtenerEstadoAsync(Url, "ARM-001");

        Assert.Null(estado);
    }

    [Fact]
    public async Task ObtenerEstado_ConCuerpoNull_DevuelveNull()
    {
        var estado = await Crear(ManejadorHttpFalso.Responde(HttpStatusCode.OK, "null"))
            .ObtenerEstadoAsync(Url, "ARM-001");

        Assert.Null(estado);
    }

    [Fact]
    public async Task ObtenerEstado_ConJsonRoto_DevuelveNull()
    {
        var estado = await Crear(ManejadorHttpFalso.Responde(HttpStatusCode.OK, "{ no es json"))
            .ObtenerEstadoAsync(Url, "ARM-001");

        Assert.Null(estado);
    }

    [Fact]
    public async Task ObtenerEstado_SiNoHayRed_DevuelveNull()
    {
        var estado = await Crear(ManejadorHttpFalso.Falla(new HttpRequestException("sin red")))
            .ObtenerEstadoAsync(Url, "ARM-001");

        Assert.Null(estado);
    }

    [Fact]
    public async Task ObtenerEstado_SiElSensorNoContestaATiempo_DevuelveNull()
    {
        var estado = await Crear(ManejadorHttpFalso.Falla(new TaskCanceledException("timeout")))
            .ObtenerEstadoAsync(Url, "ARM-001");

        Assert.Null(estado);
    }

    [Fact]
    public async Task ObtenerEstado_SiLaCancelacionVieneDeAfuera_LaDejaPasar()
    {
        using var cancelacion = new CancellationTokenSource();
        await cancelacion.CancelAsync();
        var cliente = Crear(new ManejadorHttpFalso((_, token) => Task.FromCanceled<HttpResponseMessage>(token)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cliente.ObtenerEstadoAsync(Url, "ARM-001", cancelacion.Token));
    }

    // ---------------------------------------------------------------- Órdenes

    [Fact]
    public async Task ActivarPanel_MandaUnPostAlPanelDelSensor()
    {
        var manejador = ManejadorHttpFalso.Responde(HttpStatusCode.OK);

        var resultado = await Crear(manejador).ActivarPanelAsync(Url, "ARM-001", "P01");

        Assert.Equal(ResultadoOrdenSensor.Ok, resultado);
        Assert.Equal("POST http://sensor-a:5100/sensores/ARM-001/paneles/P01/activar", Assert.Single(manejador.Pedidos));
    }

    [Fact]
    public async Task DesactivarPanel_MandaUnPostAlPanelDelSensor()
    {
        var manejador = ManejadorHttpFalso.Responde(HttpStatusCode.OK);

        await Crear(manejador).DesactivarPanelAsync(Url, "ARM-001", "P01");

        Assert.Equal("POST http://sensor-a:5100/sensores/ARM-001/paneles/P01/desactivar",
            Assert.Single(manejador.Pedidos));
    }

    [Fact]
    public async Task ActivarVolcado_MandaUnPostAlVolcadoDelSensor()
    {
        var manejador = ManejadorHttpFalso.Responde(HttpStatusCode.OK);

        await Crear(manejador).ActivarVolcadoAsync(Url, "ARM-001");

        Assert.Equal("POST http://sensor-a:5100/sensores/ARM-001/volcado/activar", Assert.Single(manejador.Pedidos));
    }

    [Fact]
    public async Task DesactivarVolcado_MandaUnPostAlVolcadoDelSensor()
    {
        var manejador = ManejadorHttpFalso.Responde(HttpStatusCode.OK);

        await Crear(manejador).DesactivarVolcadoAsync(Url, "ARM-001");

        Assert.Equal("POST http://sensor-a:5100/sensores/ARM-001/volcado/desactivar", Assert.Single(manejador.Pedidos));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, ResultadoOrdenSensor.Ok)]
    [InlineData(HttpStatusCode.Conflict, ResultadoOrdenSensor.YaAplicado)]
    [InlineData(HttpStatusCode.NotFound, ResultadoOrdenSensor.NoEncontrado)]
    [InlineData(HttpStatusCode.BadRequest, ResultadoOrdenSensor.SinRespuesta)]
    [InlineData(HttpStatusCode.InternalServerError, ResultadoOrdenSensor.SinRespuesta)]
    public async Task Orden_TraduceElCodigoDeRespuestaDelSensor(HttpStatusCode codigo, ResultadoOrdenSensor esperado)
    {
        var resultado = await Crear(ManejadorHttpFalso.Responde(codigo)).ActivarVolcadoAsync(Url, "ARM-001");

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public async Task Orden_SiNoHayRed_DevuelveSinRespuesta()
    {
        var resultado = await Crear(ManejadorHttpFalso.Falla(new HttpRequestException("sin red")))
            .ActivarPanelAsync(Url, "ARM-001", "P01");

        Assert.Equal(ResultadoOrdenSensor.SinRespuesta, resultado);
    }

    [Fact]
    public async Task Orden_SiElSensorNoContestaATiempo_DevuelveSinRespuesta()
    {
        var resultado = await Crear(ManejadorHttpFalso.Falla(new TaskCanceledException("timeout")))
            .DesactivarPanelAsync(Url, "ARM-001", "P01");

        Assert.Equal(ResultadoOrdenSensor.SinRespuesta, resultado);
    }

    [Fact]
    public async Task Orden_SiLaCancelacionVieneDeAfuera_LaDejaPasar()
    {
        using var cancelacion = new CancellationTokenSource();
        await cancelacion.CancelAsync();
        var cliente = Crear(new ManejadorHttpFalso((_, token) => Task.FromCanceled<HttpResponseMessage>(token)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cliente.ActivarVolcadoAsync(Url, "ARM-001", cancelacion.Token));
    }
}
