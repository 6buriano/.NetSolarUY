using GenerativeAI.Microsoft;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SolarUY.Application.Consultas;
using SolarUY.Application.Sensors;
using SolarUY.Domain.Repositories;
using SolarUY.Infrastructure.Consultas;
using SolarUY.Infrastructure.Mcp.Tools;
using SolarUY.Infrastructure.Persistence;
using SolarUY.Infrastructure.Repositories;
using SolarUY.Infrastructure.Sensors;

namespace SolarUY.Infrastructure;

/// <summary>
/// Registro de los servicios de infraestructura en el contenedor de dependencias.
/// Lo llaman <c>Program.cs</c> de <c>SolarUY.Web</c>, el de <c>SolarUY.Collector</c> y el de
/// <c>SolarUY.McpServer</c>, que es donde vive la configuración de IoC.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Timeout de cada consulta a un sensor. Es corto a propósito: que un sensor no responda
    /// no puede demorar la ronda de los demás (spec del simulador, sección 8).
    /// </summary>
    public static readonly TimeSpan TimeoutSensor = TimeSpan.FromSeconds(3);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SolarUYDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddScoped<IParqueRepository, ParqueRepository>();
        services.AddScoped<INodoRepository, NodoRepository>();
        services.AddScoped<IPanelRepository, PanelRepository>();
        services.AddScoped<ITelemetriaRepository, TelemetriaRepository>();
        services.AddScoped<IMantenimientoRepository, MantenimientoRepository>();

        services.AddHttpClient<ISensorClient, SensorHttpClient>(cliente =>
        {
            cliente.Timeout = TimeoutSensor;
        });

        // Cliente del inventario del simulador. Solo lo usa la verificación de vinculación, que es
        // una herramienta de diagnóstico: el producto no depende de esta API, que existe solo en el mock.
        services.AddHttpClient<ISimuladorAdminClient, SimuladorAdminHttpClient>(cliente =>
        {
            cliente.Timeout = TimeoutSensor;
        });

        return services;
    }

    /// <summary>
    /// Registra el cliente del agente que responde las consultas en lenguaje natural.
    /// Si falta la clave o el modelo, registra <see cref="AgenteNoConfigurado"/> en vez de lanzar:
    /// la web tiene que arrancar igual y avisar en el log.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="apiKey">Clave de Gemini. Si viene vacía, el agente queda no configurado.</param>
    /// <param name="modelo">Modelo de Gemini. Si viene vacío, el agente queda no configurado.</param>
    /// <param name="urlServidorMcp">URL del endpoint MCP, por ejemplo <c>http://mcp:8080/mcp</c>.</param>
    /// <param name="timeout">Tiempo máximo de una consulta completa.</param>
    public static IServiceCollection AddAgenteConsultas(this IServiceCollection services,
        string? apiKey, string? modelo, string? urlServidorMcp, TimeSpan timeout)
    {
        var faltantes = new List<string>();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            faltantes.Add("Gemini:ApiKey");
        }

        if (string.IsNullOrWhiteSpace(modelo))
        {
            faltantes.Add("Gemini:Modelo");
        }

        if (faltantes.Count > 0)
        {
            var detalle = string.Join(" y ", faltantes);

            services.AddScoped<IMCPAgentService>(proveedor => new AgenteNoConfigurado(
                proveedor.GetRequiredService<ILogger<AgenteNoConfigurado>>(), detalle));

            return services;
        }

        services.AddScoped<IMCPAgentService>(proveedor =>
        {
            var cliente = new ChatClientBuilder(
                    new GenerativeAIChatClient(apiKey!, modelo!, autoCallFunction: false))
                .UseFunctionInvocation(configure: opciones =>
                {
                    // Tope de vueltas del ciclo "modelo pide herramienta -> se ejecuta -> vuelve al
                    // modelo". Ocho alcanzan para lo que hacen estas cinco herramientas (incluso
                    // "todo hoy", que usa cuatro). El tope existe para que un modelo confundido no
                    // entre en un ida y vuelta sin fin y queme cuota sin devolver respuesta.
                    opciones.MaximumIterationsPerRequest = 8;
                })
                .Build();

            return new GeminiMcpAgentService(
                cliente,
                urlServidorMcp ?? string.Empty,
                proveedor.GetRequiredService<TimeProvider>(),
                timeout,
                proveedor.GetRequiredService<ILogger<GeminiMcpAgentService>>());
        });

        return services;
    }

    /// <summary>
    /// Registra el servidor MCP con las cinco herramientas de solo lectura.
    /// Devuelve el constructor para que el proyecto que lo aloja elija el transporte.
    /// </summary>
    public static IMcpServerBuilder AddHerramientasMcp(this IServiceCollection services)
    {
        // El argumento se declara como IEnumerable<Type> a propósito: pasando un Type[] el compilador
        // elige la sobrecarga genérica "WithTools<T>(builder, T target)" (el array encaja como target
        // por conversión de identidad) en vez de la de tipos, y el servidor arranca sin herramientas.
        IEnumerable<Type> herramientas =
        [
            typeof(ListarParquesTool),
            typeof(ObtenerResumenParqueTool),
            typeof(ObtenerGeneracionHoyTool),
            typeof(ObtenerGeneracionHistoricaTool),
            typeof(ListarPanelesEnFallaTool)
        ];

        return services.AddMcpServer().WithTools(herramientas);
    }
}
