using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolarUY.Application.Consultas;
using SolarUY.Application.Sensors;
using SolarUY.Domain.Repositories;
using SolarUY.Infrastructure.Consultas;

namespace SolarUY.Infrastructure.Tests;

/// <summary>
/// Pruebas del registro de servicios (<see cref="DependencyInjection"/>) y del agente de respaldo
/// (<see cref="AgenteNoConfigurado"/>). No se abre ninguna conexión: la cadena de MySQL y la clave de
/// Gemini son valores de mentira y lo único que se verifica es qué queda registrado y qué se resuelve.
/// </summary>
public class ConfiguracionInfraestructuraTests
{
    private const string CadenaDeConexion = "Server=localhost;Database=solaruy;User=u;Password=p";

    private static ServiceProvider ConstruirProveedor(Action<IServiceCollection> registrar)
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddSingleton(TimeProvider.System);
        registrar(servicios);
        return servicios.BuildServiceProvider();
    }

    // ---------------------------------------------------------------- AddInfrastructure

    [Fact]
    public void AddInfrastructure_RegistraLosCincoRepositoriosComoScoped()
    {
        var servicios = new ServiceCollection();

        servicios.AddInfrastructure(CadenaDeConexion);

        foreach (var contrato in new[]
                 {
                     typeof(IParqueRepository), typeof(INodoRepository), typeof(IPanelRepository),
                     typeof(ITelemetriaRepository), typeof(IMantenimientoRepository)
                 })
        {
            var registro = Assert.Single(servicios, descriptor => descriptor.ServiceType == contrato);
            Assert.Equal(ServiceLifetime.Scoped, registro.Lifetime);
        }
    }

    [Fact]
    public void AddInfrastructure_PermiteResolverLosClientesHttpDeLosSensores()
    {
        using var proveedor = ConstruirProveedor(servicios => servicios.AddInfrastructure(CadenaDeConexion));
        using var scope = proveedor.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISensorClient>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISimuladorAdminClient>());
    }

    [Fact]
    public void TimeoutSensor_EsCortoParaNoDemorarLaRondaDeLosDemasSensores()
    {
        Assert.Equal(TimeSpan.FromSeconds(3), DependencyInjection.TimeoutSensor);
    }

    // ---------------------------------------------------------------- AddAgenteConsultas

    [Theory]
    [InlineData(null, "gemini-2.5-flash", "http://mcp:8080/mcp")]
    [InlineData("clave", "", "http://mcp:8080/mcp")]
    [InlineData("clave", "gemini-2.5-flash", "   ")]
    public async Task AddAgenteConsultas_SiFaltaUnaClave_RegistraElAgenteNoConfigurado(string? apiKey,
        string? modelo, string? url)
    {
        using var proveedor = ConstruirProveedor(servicios =>
            servicios.AddAgenteConsultas(apiKey, modelo, url, TimeSpan.FromSeconds(30)));
        using var scope = proveedor.CreateScope();

        var agente = scope.ServiceProvider.GetRequiredService<IMcpAgentService>();

        Assert.IsType<AgenteNoConfigurado>(agente);
        var respuesta = await agente.ConsultarAsync("¿Cuánto se generó hoy?");
        Assert.False(respuesta.Exito);
        Assert.Equal(GeminiMcpAgentService.MensajeDeFalla, respuesta.Texto);
    }

    [Fact]
    public void AddAgenteConsultas_ConTodaLaConfiguracion_RegistraElAgenteDeGemini()
    {
        using var proveedor = ConstruirProveedor(servicios =>
            servicios.AddAgenteConsultas("clave-de-prueba", "gemini-2.5-flash", "http://mcp:8080/mcp",
                TimeSpan.FromSeconds(30)));
        using var scope = proveedor.CreateScope();

        var agente = scope.ServiceProvider.GetRequiredService<IMcpAgentService>();

        Assert.IsType<GeminiMcpAgentService>(agente);
    }

    // ---------------------------------------------------------------- AddHerramientasMcp

    [Fact]
    public void AddHerramientasMcp_RegistraElServidorMcp()
    {
        var servicios = new ServiceCollection();

        var constructor = servicios.AddHerramientasMcp();

        Assert.NotNull(constructor);
        Assert.NotEmpty(servicios);
    }

    // ---------------------------------------------------------------- AgenteNoConfigurado

    [Fact]
    public void AgenteNoConfigurado_AlCrearse_AvisaEnElLogQueClavesFaltan()
    {
        var logger = new LoggerQueAnota<AgenteNoConfigurado>();

        _ = new AgenteNoConfigurado(logger, "Gemini:ApiKey y Mcp:UrlServidor");

        var aviso = Assert.Single(logger.Mensajes);
        Assert.Equal(LogLevel.Warning, aviso.Nivel);
        Assert.Contains("Gemini:ApiKey y Mcp:UrlServidor", aviso.Texto);
    }

    [Fact]
    public async Task AgenteNoConfigurado_Consultar_DevuelveElMensajeDeFallaSinLlamarAAfuera()
    {
        var agente = new AgenteNoConfigurado(new LoggerQueAnota<AgenteNoConfigurado>(), "Gemini:ApiKey");

        var respuesta = await agente.ConsultarAsync("cualquier pregunta");

        Assert.False(respuesta.Exito);
        Assert.Equal(GeminiMcpAgentService.MensajeDeFalla, respuesta.Texto);
    }

    private sealed class LoggerQueAnota<T> : ILogger<T>
    {
        public List<(LogLevel Nivel, string Texto)> Mensajes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Mensajes.Add((logLevel, formatter(state, exception)));
    }
}
