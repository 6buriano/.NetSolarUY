using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.Services;

namespace SolarUY.Web.Controllers;

/// <summary>
/// Herramientas de diagnóstico para desarrollo y para la demo. No forman parte del producto:
/// dependen del inventario del simulador, que es una API que solo existe en el mock.
/// </summary>
public class DiagnosticoController : Controller
{
    private readonly VerificacionVinculacionService _verificacionService;
    private readonly IWebHostEnvironment _entorno;

    public DiagnosticoController(VerificacionVinculacionService verificacionService, IWebHostEnvironment entorno)
    {
        _verificacionService = verificacionService;
        _entorno = entorno;
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

        var informe = await _verificacionService.VerificarAsync(cancellationToken);
        return View(informe);
    }
}
