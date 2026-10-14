using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.Services;

namespace SolarUY.Web.Controllers;

/// <summary>
/// Herramientas de diagnóstico para desarrollo y para la demo. No forman parte del producto:
/// dependen del inventario del simulador, que es una API que solo existe en el mock.
/// </summary>
public class DiagnosticoController : Controller
{
    private const string UrlSimuladorPorDefecto = "http://localhost:5100";

    private readonly VerificacionVinculacionService _verificacionService;
    private readonly IWebHostEnvironment _entorno;
    private readonly string _urlSimulador;

    public DiagnosticoController(VerificacionVinculacionService verificacionService,
        IWebHostEnvironment entorno, IConfiguration configuracion)
    {
        _verificacionService = verificacionService;
        _entorno = entorno;
        _urlSimulador = configuracion["Simulador:UrlBase"] ?? UrlSimuladorPorDefecto;
    }

    /// <summary>
    /// Compara los nodos, sensores y paneles cargados contra el inventario del simulador y lista
    /// todo lo que no cierra. Solo existe en desarrollo.
    /// </summary>
    public async Task<IActionResult> Vinculacion(CancellationToken cancellationToken)
    {
        if (!_entorno.IsDevelopment())
        {
            return NotFound();
        }

        var informe = await _verificacionService.VerificarAsync(_urlSimulador, cancellationToken);

        ViewData["UrlSimulador"] = _urlSimulador;
        ViewData["TituloPagina"] = "Verificación de vinculación";

        return View(informe);
    }
}
