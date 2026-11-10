using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.Services;
using SolarUY.Web.Models;

namespace SolarUY.Web.Controllers;

/// <summary>Pantalla de monitoreo en tiempo real (req012). Mirar no modifica nada: la ve también Consulta.</summary>
[Authorize(Roles = "Administrador,Consulta")]
public class MonitoreoController : Controller
{
    private readonly MonitoreoService _monitoreoService;

    public MonitoreoController(MonitoreoService monitoreoService)
    {
        _monitoreoService = monitoreoService;
    }

    public async Task<IActionResult> Index(Guid? parqueId, CancellationToken cancellationToken)
    {
        // La primera foto se arma acá, así la pantalla muestra datos al abrir sin esperar al worker.
        var (instantanea, _) = await _monitoreoService.ObtenerInstantaneaAsync(cancellationToken);

        var parque = instantanea.Parques.FirstOrDefault(p => p.ParqueId == parqueId)
                     ?? instantanea.Parques.FirstOrDefault();

        return View(new MonitoreoIndexViewModel { Instantanea = instantanea, Parque = parque });
    }
}