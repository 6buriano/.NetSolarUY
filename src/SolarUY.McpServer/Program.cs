using SolarUY.Application.Services;
using SolarUY.Infrastructure;
using SolarUY.Infrastructure.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SolarUY")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'SolarUY'. En desarrollo va en appsettings.Development.json; " +
        "en producción se define con la variable de entorno ConnectionStrings__SolarUY.");

// Inversión de dependencias: DbContext y repositorios, que es lo que necesitan los servicios de abajo.
builder.Services.AddInfrastructure(connectionString);

// Servicios de aplicación que consumen las herramientas, y solo esos: los tres que piden constructor.
builder.Services.AddScoped<ParqueService>();
builder.Services.AddScoped<PanelService>();
builder.Services.AddScoped<ReporteService>();

// El reloj como dependencia de los casos de uso: en producción es el del sistema. Se registra acá
// y no en cada servicio para que el contenedor lo resuelva igual en todos.
builder.Services.AddSingleton(TimeProvider.System);

// Servidor MCP: transporte HTTP sin estado, para que cada pedido se pueda atender en cualquier
// instancia sin compartir sesión, y las cinco herramientas de solo lectura.
//
// El argumento se declara como IEnumerable<Type> a propósito: pasando un Type[] el compilador elige
// la sobrecarga genérica "WithTools<T>(builder, T target)" (el array encaja como target por
// conversión de identidad) en vez de la de tipos, y el servidor arranca sin herramientas.
IEnumerable<Type> herramientas =
[
    typeof(ListarParquesTool),
    typeof(ObtenerResumenParqueTool),
    typeof(ObtenerGeneracionHoyTool),
    typeof(ObtenerGeneracionHistoricaTool),
    typeof(ListarPanelesEnFallaTool)
];

builder.Services
    .AddMcpServer()
    .WithHttpTransport(opciones => opciones.Stateless = true)
    .WithTools(herramientas);

var app = builder.Build();

// El servidor MCP cuelga de /mcp. Sin autenticación y sin publicar puerto: solo lo alcanzan los
// contenedores de la red de Docker, igual que al colector.
app.MapMcp("/mcp");

// Liveness: solo dice que el proceso responde. No consulta MySQL a propósito.
app.MapGet("/health", () => Results.Ok(new { status = "ok", machine = Environment.MachineName }));

app.Run();
