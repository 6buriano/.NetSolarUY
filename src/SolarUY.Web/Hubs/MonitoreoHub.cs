using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SolarUY.Web.Hubs;

/// <summary>
/// Conexión en vivo del monitoreo (req012). Los navegadores se suscriben a un grupo y el
/// MonitoreoTiempoRealWorker les envía la foto del parque, las lecturas nuevas y los avisos de falla.
/// </summary>
[Authorize(Roles = "Administrador,Consulta")]
public class MonitoreoHub : Hub
{
    // Nombres de los mensajes que manda el servidor. Los usa el worker para enviar
    // y el JavaScript para escuchar: tienen que coincidir exactamente.
    public const string MensajeInstantanea = "instantanea";
    public const string MensajeFalla = "falla";
    public const string MensajeLectura = "lectura";

    public const string GrupoMonitoreo = "monitoreo";

    public static string GrupoPanel(Guid panelId) => $"panel-{panelId}";

    /// <summary>La pantalla de Monitoreo pide recibir la foto del parque y los avisos de falla.</summary>
    public Task SuscribirseMonitoreo()
        => Groups.AddToGroupAsync(Context.ConnectionId, GrupoMonitoreo);

    /// <summary>
    /// El detalle de un panel pide recibir sus lecturas nuevas. Solo Administrador, igual que la
    /// pantalla de detalle (PanelesController).
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public Task SuscribirsePanel(Guid panelId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GrupoPanel(panelId));
}