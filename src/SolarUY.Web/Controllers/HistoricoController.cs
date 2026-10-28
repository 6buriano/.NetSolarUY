using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.Services;
using SolarUY.Domain.Enums;
using SolarUY.Web.Models;

namespace SolarUY.Web.Controllers;

/// <summary>
/// Consultas históricas: generación de un panel agrupada por día, mes o año.
/// </summary>
[Authorize(Roles = "Administrador")]
public class HistoricoController : Controller
{
    private readonly ReporteService _reporteService;
    private readonly PanelService _panelService;

    public HistoricoController(ReporteService reporteService, PanelService panelService)
    {
        _reporteService = reporteService;
        _panelService = panelService;
    }

    public async Task<IActionResult> Index(Guid? panelId, GranularidadConsulta granularidad = GranularidadConsulta.DIA,
        DateTime? desde = null, DateTime? hasta = null, CancellationToken cancellationToken = default)
    {
        var paneles = await _panelService.ObtenerTodosAsync(cancellationToken: cancellationToken);

        // Sin panel elegido todavía, se propone el primero: la pantalla siempre necesita uno para
        // tener algo que mostrar, a diferencia de Mantenimiento, donde "todos" tiene sentido.
        var panelElegido = panelId ?? paneles.FirstOrDefault()?.Id;

        var resultados = panelElegido is null
            ? []
            : await _reporteService.ObtenerGeneracionPorPanelAsync(panelElegido.Value, granularidad,
                desde, hasta?.Date.AddDays(1).AddTicks(-1), cancellationToken);

        return View(new HistoricoIndexViewModel
        {
            Paneles = paneles,
            PanelSolarId = panelElegido,
            Granularidad = granularidad,
            Desde = desde,
            Hasta = hasta,
            Resultados = resultados
        });
    }
}
