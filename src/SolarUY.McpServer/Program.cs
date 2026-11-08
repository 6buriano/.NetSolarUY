// Punto de entrada provisorio. Un proyecto con Microsoft.NET.Sdk.Web necesita un Main para compilar,
// así que queda este mínimo; la tarea 4.12 lo reemplaza con la cadena de conexión, AddInfrastructure,
// los servicios de Application, las 5 herramientas MCP y el endpoint HTTP sin sesión.
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Run();
