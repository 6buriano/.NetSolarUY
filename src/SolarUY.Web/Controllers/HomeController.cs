using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SolarUY.Web.Models;

namespace SolarUY.Web.Controllers;

public class HomeController : Controller
{
      // La página de inicio es el monitoreo en tiempo real (req012), no la de la plantilla.
    public IActionResult Index() => RedirectToAction("Index", "Monitoreo");

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
