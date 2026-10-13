using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolarUY.Application.Sensors;
using SolarUY.Domain.Repositories;
using SolarUY.Infrastructure.Persistence;
using SolarUY.Infrastructure.Repositories;
using SolarUY.Infrastructure.Sensors;

namespace SolarUY.Infrastructure;

/// <summary>
/// Registro de los servicios de infraestructura en el contenedor de dependencias.
/// Lo llama <c>Program.cs</c> de <c>SolarUY.Web</c> y el de <c>SolarUY.Collector</c>,
/// que es donde vive la configuración de IoC.
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

        return services;
    }
}
