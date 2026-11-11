using Microsoft.AspNetCore.Mvc;

namespace SolarUY.Web.Controllers;

/// <summary>
/// Ayuda del sistema. Las explicaciones largas (qué significa una estado, cómo se estima la energía,
/// qué es el sensor del nodo) viven acá y no en las pantallas: quien opera el sistema todos los días
/// no necesita leerlas de nuevo cada vez.
/// </summary>
public class AyudaController : Controller
{
    public IActionResult Index() => View();
}
