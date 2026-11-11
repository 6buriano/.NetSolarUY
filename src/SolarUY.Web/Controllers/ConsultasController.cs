using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Exceptions;
using SolarUY.Web.Models;

namespace SolarUY.Web.Controllers;

/// <summary>
/// Consultas en lenguaje natural: la pregunta viaja al agente y la respuesta se muestra en la
/// misma pantalla.
/// </summary>
[Authorize(Roles = "Administrador")]
public class ConsultasController : Controller
{
    private readonly ConsultaService _consultaService;

    public ConsultasController(ConsultaService consultaService)
    {
        _consultaService = consultaService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ConsultaViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ConsultaViewModel modelo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            // Se devuelve lo que escribió el usuario junto con los errores, sin llamar al agente.
            return View(modelo);
        }

        try
        {
            var respuesta = await _consultaService.ConsultarAsync(
                new ConsultaLenguajeNaturalRequest(modelo.Pregunta), cancellationToken);

            modelo.PreguntaEnviada = modelo.Pregunta.Trim();
            modelo.Respuesta = respuesta.Texto;
            modelo.Exito = respuesta.Exito;

            if (respuesta.Exito)
            {
                // Solo se limpia cuando hubo respuesta: el usuario no tiene que borrar a mano la
                // pregunta anterior para hacer la siguiente. Se quita del ModelState porque el Tag
                // Helper lee de ahí, no del modelo, y volvería a mostrar el texto viejo.
                modelo.Pregunta = string.Empty;
                ModelState.Remove(nameof(ConsultaViewModel.Pregunta));
            }

            // Si el agente falló (Exito en falso), la pregunta se conserva para poder reintentar sin
            // volver a escribirla.
        }
        catch (ReglaDeNegocioException excepcion)
        {
            // Pregunta vacía o demasiado larga: el mensaje está escrito para el usuario, así que va
            // al formulario junto al campo, y el error queda en el ModelState. El texto escrito se
            // conserva para que pueda corregirlo. No es una falla del sistema.
            ModelState.AddModelError(nameof(ConsultaViewModel.Pregunta), excepcion.Message);
        }

        // No se redirige: la acción no modifica datos y redirigir obligaría a guardar la respuesta en
        // TempData. Se vuelve a la misma vista con el modelo completo.
        return View(modelo);
    }
}
