using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Enums;

namespace SolarUY.Web.Seed;

/// <summary>
/// Datos de ejemplo para desarrollo: un parque, un nodo con su inversor y sensor, y los 5
/// paneles (P01…P05, 300 W) que trae por defecto el simulador para ARM-001. Reproduce a mano
/// lo mismo que pide <c>Documentacion/PRUEBA-RAPIDA-SolarUY.md</c>, así que al clonar y levantar
/// el entorno el colector ya tiene algo que leer sin pasar primero por el ABM.
///
/// Pasa por los servicios de Application (no por EF Core <c>HasData</c>) para que los datos
/// respeten las mismas reglas de negocio que el ABM: <c>HasData</c> necesitaría IDs fijos y
/// bypasea los constructores del dominio, que es justo donde viven esas reglas.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider servicios, string urlSimulador,
        CancellationToken cancellationToken = default)
    {
        var parqueService = servicios.GetRequiredService<ParqueService>();
        var nodoService = servicios.GetRequiredService<NodoService>();
        var panelService = servicios.GetRequiredService<PanelService>();

        // Idempotente: si ya hay algo cargado (por ejemplo, un clon que ya tiene datos de una
        // sesión anterior), no duplica nada.
        var parques = await parqueService.ObtenerTodosAsync(cancellationToken);
        if (parques.Count > 0)
        {
            return;
        }

        var parqueId = await parqueService.CrearAsync(
            new CrearParqueRequest("Parque Demo", "Salto", 1.5, DateTime.UtcNow.Date),
            cancellationToken);

        var nodoId = await nodoService.CrearAsync(
            new CrearNodoRequest(
                "Nodo 1",
                "Norte",
                parqueId,
                new InversorRequest("SMA", "SN-1", 97.5, true),
                new SensorDispositivoRequest("ARM-001", "1.0.0", urlSimulador, true)),
            cancellationToken);

        for (var i = 1; i <= 5; i++)
        {
            await panelService.CrearAsync(
                new CrearPanelRequest(
                    $"P{i:D2}",
                    "JAM72S30-550/MR",
                    "JA Solar",
                    300,
                    EstadoSalud.OPERATIVO,
                    Activo: true,
                    DateTime.UtcNow.Date,
                    nodoId),
                cancellationToken);
        }
    }
}
