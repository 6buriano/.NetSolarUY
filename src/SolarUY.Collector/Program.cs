using SolarUY.Collector;
using SolarUY.Collector.Configuration;
using SolarUY.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ColectorOptions>(builder.Configuration.GetSection(ColectorOptions.Seccion));

var connectionString = builder.Configuration.GetConnectionString("SolarUY")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'SolarUY'. En desarrollo va en appsettings.Development.json; " +
        "en producción se define con la variable de entorno ConnectionStrings__SolarUY.");

// Inversión de dependencias: DbContext, repositorios y el cliente HTTP que consulta a los sensores.
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddHostedService<ColectorTelemetriaWorker>();

var host = builder.Build();
host.Run();
