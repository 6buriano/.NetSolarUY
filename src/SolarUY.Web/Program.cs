using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using SolarUY.Application.Sensors;
using SolarUY.Application.Services;
using SolarUY.Infrastructure;
using SolarUY.Infrastructure.Persistence;
using SolarUY.Infrastructure.Sensors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Los formularios HTML envían siempre los decimales con punto y las fechas en ISO-8601,
// sin importar la configuración regional del equipo. Si dejamos la cultura del sistema,
// en un equipo configurado para Uruguay (es-UY, separador decimal la coma) el valor "120.5"
// se interpreta como 1205. Por eso fijamos la cultura de la petición en la invariante.
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
    options.SupportedCultures = [CultureInfo.InvariantCulture];
    options.SupportedUICultures = [CultureInfo.InvariantCulture];
});

var connectionString = builder.Configuration.GetConnectionString("SolarUY")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'SolarUY'. En desarrollo va en appsettings.Development.json; " +
        "en producción se define con la variable de entorno ConnectionStrings__SolarUY.");

// Inversión de dependencias: acá se conectan las interfaces con sus implementaciones.
builder.Services.AddInfrastructure(connectionString);

// Servicios de aplicación (casos de uso) que consumen los controladores.
builder.Services.AddScoped<ParqueService>();
builder.Services.AddScoped<NodoService>();
builder.Services.AddScoped<PanelService>();

// Herramienta de diagnóstico: compara el sistema contra el inventario del simulador.
// Solo se usa desde DiagnosticoController, que está limitado a desarrollo.
builder.Services.AddScoped<VerificacionVinculacionService>();

// Verificación y alta de sensores contra el hardware. En desarrollo se le pregunta al simulador si
// el identificador que cargó el usuario existe, y se le puede pedir que lo cree. En cualquier otro
// entorno se registra la implementación nula: no hay inventario que consultar, así que el ABM crea
// el nodo con el identificador que le den, como siempre.
var urlSimulador = builder.Configuration["Simulador:UrlBase"] ?? "http://localhost:5100";

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ISensorDisponibilidadService>(proveedor =>
        new SensorDisponibilidadSimulador(
            proveedor.GetRequiredService<ISimuladorAdminClient>(),
            urlSimulador,
            proveedor.GetRequiredService<ILogger<SensorDisponibilidadSimulador>>()));
}
else
{
    builder.Services.AddScoped<ISensorDisponibilidadService, SensorDisponibilidadNula>();
}

// TODO (etapa de seguridad, specs.md §4.2 y §5.1): cuando se integre Keycloak se agrega
// AddAuthentication().AddOpenIdConnect(), el mapeo del rol y [Authorize(Roles = "Administrador")]
// sobre ParquesController, NodosController y PanelesController.

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Aplica las migraciones pendientes al arrancar, para que nadie se olvide de correrlas.
    // En producción las migraciones se aplican aparte, porque hay varias instancias de la app.
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SolarUYDbContext>();
    context.Database.Migrate();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Aplica la cultura de la petición antes del binding de modelos.
app.UseRequestLocalization();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
