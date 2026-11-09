using SolarUY.Application.Services;
using SolarUY.Infrastructure;

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

// Servidor MCP: las cinco herramientas (registradas desde Infrastructure) con transporte HTTP sin
// estado, para que cada pedido se pueda atender en cualquier instancia sin compartir sesión.
builder.Services
    .AddHerramientasMcp()
    .WithHttpTransport(opciones => opciones.Stateless = true);

var app = builder.Build();

// El servidor MCP cuelga de /mcp. Sin autenticación y sin publicar puerto: solo lo alcanzan los
// contenedores de la red de Docker, igual que al colector.
app.MapMcp("/mcp");

// Liveness: solo dice que el proceso responde. No consulta MySQL a propósito.
app.MapGet("/health", () => Results.Ok(new { status = "ok", machine = Environment.MachineName }));

app.Run();
