using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using SolarUY.Application.Sensors;
using SolarUY.Application.Services;
using SolarUY.Infrastructure;
using SolarUY.Infrastructure.Persistence;
using SolarUY.Infrastructure.Sensors;
using SolarUY.Web.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Las dos réplicas de la web tienen que cifrar y descifrar con las mismas claves: si no, al
// pasar de una a otra se invalidan los formularios (antiforgery) y el TempData, que va en
// cookie cifrada. En Docker las claves viven en un volumen compartido; con dotnet run sobre el
// host no hay carpeta configurada y se usa la del perfil del usuario, como hasta ahora.
var proteccionDeDatos = builder.Services.AddDataProtection().SetApplicationName("SolarUY.Web");

var carpetaClaves = builder.Configuration["DataProtection:Directory"];
if (!string.IsNullOrWhiteSpace(carpetaClaves))
{
    proteccionDeDatos.PersistKeysToFileSystem(new DirectoryInfo(carpetaClaves));
}

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

// Autenticación con Keycloak (OpenID Connect). Cómo funciona:
// - Si alguien sin sesión entra a una página protegida, se lo manda a Keycloak a loguearse.
// - Keycloak lo devuelve a /signin-oidc con un código, y la web lo canjea por los datos del usuario.
// - La web guarda la sesión en una cookie, así no vuelve a Keycloak en cada pedido.

//aca abajo lo armo en guille-keycloak
var keycloak = builder.Configuration.GetSection("Keycloak");

builder.Services
    .AddAuthentication(opciones =>
    {
        // La sesión se guarda en una cookie...
        opciones.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        // ...y si no hay sesión, se le pide el login a Keycloak.
        opciones.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(opciones =>
    {
        // Logueado pero sin el rol necesario: se muestra esta página en lugar de un error.
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    })
    .AddOpenIdConnect(opciones =>
    {
        opciones.Authority = keycloak["Authority"];
        opciones.ClientId = keycloak["ClientId"];
        opciones.ClientSecret = keycloak["ClientSecret"]; // viene de user secrets, no del repo
        opciones.ResponseType = "code";                   // flujo Authorization Code, el recomendado
        opciones.SaveTokens = true;                       // guarda el token: hace falta para cerrar sesión
        opciones.GetClaimsFromUserInfoEndpoint = true;    // pide a Keycloak los datos del usuario

        // Usar los nombres de los datos tal como los manda Keycloak, sin traducirlos a los de Microsoft.
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters.NameClaimType = "preferred_username";
        opciones.TokenValidationParameters.RoleClaimType = "roles";
        opciones.ClaimActions.MapJsonKey("roles", "roles");
    });

var app = builder.Build();

// Paso de arranque (docker compose --profile migracion run --rm migracion): aplica el esquema,
// crea el anillo de claves y termina, sin levantar el servidor. Es la única forma en que se
// migra en el despliegue: las réplicas no migran al arrancar, para que dos instancias no
// compitan por el mismo esquema.
if (args.Contains("--migrate-only"))
{
    using var alcanceMigracion = app.Services.CreateScope();
    alcanceMigracion.ServiceProvider.GetRequiredService<SolarUYDbContext>().Database.Migrate();

    // El anillo de claves se crea acá, con un solo proceso y antes de que arranquen las réplicas.
    // Si las dos arrancaran con el volumen vacío, cada una generaría la suya y una no podría
    // descifrar los tokens de la otra hasta el refresco del anillo (24 h por defecto). Si la
    // carpeta no se puede escribir, esto lanza y el paso termina con error: es preferible frenar
    // el despliegue a quedar con dos réplicas que no comparten la clave.
    app.Services.GetRequiredService<IDataProtectionProvider>()
        .CreateProtector("SolarUY.Bootstrap")
        .Protect("bootstrap");

    app.Logger.LogInformation(
        "Migraciones aplicadas y anillo de claves creado (--migrate-only). Termina el proceso.");
    return;
}

// Primero de todo el pipeline: nginx manda X-Forwarded-For/Proto/Host. Sin esto la app cree que
// todos los pedidos llegan por HTTP y desde el mismo cliente.
// KnownIPNetworks: solo se aceptan las cabeceras que vienen de la red de Docker (la de nginx).
// Vaciar la lista (o usar ASPNETCORE_FORWARDEDHEADERS_ENABLED) haría que cualquiera que llegue
// al puerto pueda falsificar su IP y decir que el pedido es https.
var opcionesReenviadas = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                     | ForwardedHeaders.XForwardedProto
                     | ForwardedHeaders.XForwardedHost,
    ForwardLimit = 1,
};

var redConfiable = builder.Configuration["ReverseProxy:KnownNetwork"];
if (!string.IsNullOrWhiteSpace(redConfiable))
{
    opcionesReenviadas.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(redConfiable));
}

app.UseForwardedHeaders(opcionesReenviadas);

if (app.Environment.IsDevelopment())
{
    // Aplica las migraciones pendientes al arrancar, salvo que el entorno lo desactive: en
    // Docker las aplica el paso "migracion", una sola vez, y las réplicas arrancan sin migrar.
    if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SolarUYDbContext>();
        context.Database.Migrate();
    }

    // Datos de ejemplo para no arrancar con el ABM vacío: un parque, un nodo con su sensor
    // (ARM-001) y los 5 paneles que trae por defecto el simulador. No hace nada si ya hay datos.
    if (builder.Configuration.GetValue("Database:SeedOnStartup", true))
    {
        using var scope = app.Services.CreateScope();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider, urlSimulador);
    }
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// El TLS termina en nginx y la app se entera por X-Forwarded-Proto: redirigir acá no hace falta
// y, si la cabecera no llegara, sería un bucle. En Docker se apaga por configuración.
if (builder.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}

// Aplica la cultura de la petición antes del binding de modelos.
app.UseRequestLocalization();
app.UseRouting();

//agrego autenticacion guille-keycloak
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Liveness: solo dice que el proceso responde. A propósito no consulta MySQL, para que un
// reinicio de la base no reinicie las réplicas. "machine" es el hostname del contenedor
// (web1/web2), así el healthcheck y el despliegue dicen qué réplica contestó.
// AllowAnonymous: cuando entre Keycloak, /health tiene que quedar abierto.
app.MapGet("/health", () => Results.Ok(new { status = "ok", machine = Environment.MachineName }))
    .AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
