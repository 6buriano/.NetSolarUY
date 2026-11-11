using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using SolarUY.Application.Sensors;
using SolarUY.Infrastructure;
using SolarUY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SolarUY.IntegrationTests;

/// <summary>
/// Una base MySQL descartable para un test: se crea vacía, se le aplican las migraciones reales del
/// proyecto y se borra al terminar. Cada test tiene la suya, así que pueden correr en paralelo y no
/// se pisan entre sí ni con datos de desarrollo.
/// </summary>
/// <remarks>
/// Se usa MySQL de verdad y no un sustituto en memoria porque lo que se quiere probar es justamente
/// lo que depende del motor: la traducción de los <c>GroupBy</c> por año/mes/día, el
/// <c>ExecuteDelete</c> de la retención y las migraciones.
/// </remarks>
public sealed class EntornoMySql : IAsyncLifetime
{
    public const string Variable = "SOLARUY_TEST_MYSQL";

    public static bool Disponible => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable));

    private readonly string _nombreBase = $"solaruy_it_{Guid.NewGuid():N}";
    private string _conexionServidor = string.Empty;

    public SensorClientFalso Sensores { get; } = new();

    public ServiceProvider Servicios { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (!Disponible)
        {
            return; // Los tests están saltados: no hay nada que preparar.
        }

        _conexionServidor = Environment.GetEnvironmentVariable(Variable)!;

        await using (var conexion = new MySqlConnection(_conexionServidor))
        {
            await conexion.OpenAsync();
            await using var crear = conexion.CreateCommand();
            crear.CommandText = $"CREATE DATABASE `{_nombreBase}` CHARACTER SET utf8mb4";
            await crear.ExecuteNonQueryAsync();
        }

        var conexionBase = new MySqlConnectionStringBuilder(_conexionServidor) { Database = _nombreBase }
            .ConnectionString;

        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddInfrastructure(conexionBase);

        // El sensor es la única dependencia externa: se reemplaza por uno que contesta lo que el test diga.
        servicios.AddSingleton<ISensorClient>(Sensores);

        servicios.AddSingleton<TimeProvider>(Reloj);
        servicios.AddScoped<SolarUY.Application.Services.ParqueService>();
        servicios.AddScoped<SolarUY.Application.Services.NodoService>();
        servicios.AddScoped<SolarUY.Application.Services.PanelService>();
        servicios.AddScoped<SolarUY.Application.Services.ReporteService>();
        servicios.AddScoped<SolarUY.Application.Services.RetencionTelemetriaService>();

        Servicios = servicios.BuildServiceProvider();

        using var scope = Servicios.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SolarUYDbContext>().Database.MigrateAsync();
    }

    /// <summary>Reloj del test; por defecto, el 11/11/2026 a las 15:00 UTC.</summary>
    public RelojFijo Reloj { get; } = new(new DateTimeOffset(2026, 11, 11, 15, 0, 0, TimeSpan.Zero));

    public async Task DisposeAsync()
    {
        if (Servicios is not null)
        {
            await Servicios.DisposeAsync();
        }

        if (!Disponible)
        {
            return;
        }

        // Los pools de conexiones mantienen abiertas conexiones a la base: sin esto el DROP puede quedar esperando.
        MySqlConnection.ClearAllPools();

        await using var conexion = new MySqlConnection(_conexionServidor);
        await conexion.OpenAsync();
        await using var borrar = conexion.CreateCommand();
        borrar.CommandText = $"DROP DATABASE IF EXISTS `{_nombreBase}`";
        await borrar.ExecuteNonQueryAsync();
    }
}

/// <summary>Reloj que se puede mover a mano, para probar "hoy" y la retención sin depender de la hora real.</summary>
public sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    private DateTimeOffset _ahora = ahora;

    public override DateTimeOffset GetUtcNow() => _ahora;

    public void Fijar(DateTimeOffset ahora) => _ahora = ahora;
}
