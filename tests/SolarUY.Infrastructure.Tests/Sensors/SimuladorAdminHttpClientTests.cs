using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.Infrastructure.Sensors;

namespace SolarUY.Infrastructure.Tests.Sensors;

/// <summary>
/// Pruebas de <see cref="SimuladorAdminHttpClient"/>: inventario y alta de sensores del simulador.
/// Cualquier falla se traduce en <c>null</c> y queda en el log; el resto del sistema no ve excepciones.
/// </summary>
public class SimuladorAdminHttpClientTests
{
    private const string Url = "http://simulador:5100/";

    private const string InventarioJson = """
        [
          { "identificadorHardwareARM": "ARM-001", "volcadoRedActivo": true,
            "paneles": [
              { "codigoPanel": "P01", "potenciaNominalW": 550, "activo": true, "falla": null, "fallaForzada": false },
              { "codigoPanel": "P02", "potenciaNominalW": 550, "activo": false, "falla": "sombra", "fallaForzada": true }
            ] }
        ]
        """;

    private static SimuladorAdminHttpClient Crear(ManejadorHttpFalso manejador)
        => new(new HttpClient(manejador), NullLogger<SimuladorAdminHttpClient>.Instance);

    // ---------------------------------------------------------------- Inventario

    [Fact]
    public async Task ObtenerInventario_ConRespuestaValida_DevuelveLosSensoresYSusPaneles()
    {
        var manejador = ManejadorHttpFalso.Responde(HttpStatusCode.OK, InventarioJson);

        var inventario = await Crear(manejador).ObtenerInventarioAsync(Url);

        Assert.NotNull(inventario);
        var sensor = Assert.Single(inventario);
        Assert.Equal("ARM-001", sensor.IdentificadorHardwareARM);
        Assert.True(sensor.VolcadoRedActivo);
        Assert.Equal(2, sensor.Paneles.Count);
        Assert.Equal("sombra", sensor.Paneles[1].Falla);
        Assert.True(sensor.Paneles[1].FallaForzada);
        Assert.Equal("GET http://simulador:5100/simulador/inventario", Assert.Single(manejador.Pedidos));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ObtenerInventario_ConRespuestaDeError_DevuelveNull(HttpStatusCode codigo)
    {
        Assert.Null(await Crear(ManejadorHttpFalso.Responde(codigo)).ObtenerInventarioAsync(Url));
    }

    [Fact]
    public async Task ObtenerInventario_ConJsonRoto_DevuelveNull()
    {
        Assert.Null(await Crear(ManejadorHttpFalso.Responde(HttpStatusCode.OK, "[ roto"))
            .ObtenerInventarioAsync(Url));
    }

    [Fact]
    public async Task ObtenerInventario_SiNoHayRed_DevuelveNull()
    {
        Assert.Null(await Crear(ManejadorHttpFalso.Falla(new HttpRequestException("sin red")))
            .ObtenerInventarioAsync(Url));
    }

    [Fact]
    public async Task ObtenerInventario_SiNoRespondeATiempo_DevuelveNull()
    {
        Assert.Null(await Crear(ManejadorHttpFalso.Falla(new TaskCanceledException("timeout")))
            .ObtenerInventarioAsync(Url));
    }

    [Fact]
    public async Task ObtenerInventario_SiLaCancelacionVieneDeAfuera_LaDejaPasar()
    {
        using var cancelacion = new CancellationTokenSource();
        await cancelacion.CancelAsync();
        var cliente = Crear(new ManejadorHttpFalso((_, token) => Task.FromCanceled<HttpResponseMessage>(token)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cliente.ObtenerInventarioAsync(Url, cancelacion.Token));
    }

    // ---------------------------------------------------------------- Alta de sensor

    [Fact]
    public async Task CrearSensor_Manda_ElPedidoConLaTopologiaYDevuelveElSensorCreado()
    {
        var manejador = ManejadorHttpFalso.Responde(HttpStatusCode.Created, """
            { "identificadorHardwareARM": "ARM-009", "volcadoRedActivo": true,
              "paneles": [ { "codigoPanel": "P01", "potenciaNominalW": 300, "activo": true,
                             "falla": null, "fallaForzada": false } ] }
            """);

        var creado = await Crear(manejador).CrearSensorAsync(Url, "ARM-009", 3, 300d);

        Assert.NotNull(creado);
        Assert.Equal("ARM-009", creado.IdentificadorHardwareARM);
        Assert.Equal("POST http://simulador:5100/simulador/sensores", Assert.Single(manejador.Pedidos));

        using var cuerpo = JsonDocument.Parse(Assert.Single(manejador.Cuerpos)!);
        Assert.Equal("ARM-009", cuerpo.RootElement.GetProperty("identificadorHardwareARM").GetString());
        Assert.Equal(3, cuerpo.RootElement.GetProperty("cantidadPaneles").GetInt32());
        Assert.Equal(300d, cuerpo.RootElement.GetProperty("potenciaNominalW").GetDouble(), 6);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task CrearSensor_ConRespuestaDeError_DevuelveNull(HttpStatusCode codigo)
    {
        Assert.Null(await Crear(ManejadorHttpFalso.Responde(codigo)).CrearSensorAsync(Url, "ARM-009", 3, 300d));
    }

    [Fact]
    public async Task CrearSensor_ConJsonRoto_DevuelveNull()
    {
        Assert.Null(await Crear(ManejadorHttpFalso.Responde(HttpStatusCode.Created, "{ roto"))
            .CrearSensorAsync(Url, "ARM-009", 3, 300d));
    }

    [Fact]
    public async Task CrearSensor_SiNoHayRed_DevuelveNull()
    {
        Assert.Null(await Crear(ManejadorHttpFalso.Falla(new HttpRequestException("sin red")))
            .CrearSensorAsync(Url, "ARM-009", 3, 300d));
    }

    [Fact]
    public async Task CrearSensor_SiNoRespondeATiempo_DevuelveNull()
    {
        Assert.Null(await Crear(ManejadorHttpFalso.Falla(new TaskCanceledException("timeout")))
            .CrearSensorAsync(Url, "ARM-009", 3, 300d));
    }
}
