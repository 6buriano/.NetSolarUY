using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SolarUY.Web.Controllers;

/// <summary>
/// Inicio y cierre de sesión con Keycloak. No tiene lógica propia: el login lo hace Keycloak,
/// y acá solo se le pide que empiece o que termine la sesión.
/// </summary>
[AllowAnonymous]
public class CuentaController : Controller
{
    /// <summary>
    /// Manda al usuario a la página de login de Keycloak. Cuando vuelve, lo lleva a donde quería ir.
    /// </summary>
    public IActionResult IniciarSesion(string? returnUrl = null)
    {
        // Solo se acepta una dirección de nuestra propia web, para que nadie use este enlace
        // para redirigir a un usuario a un sitio externo.
        var destino = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home");

        return Challenge(new AuthenticationProperties { RedirectUri = destino },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Cierra la sesión en la web (borra la cookie) y también en Keycloak. Si solo se borrara la
    /// cookie, al volver a "Iniciar sesión" Keycloak te dejaría entrar sin pedir la contraseña.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CerrarSesion()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>El usuario inició sesión pero no tiene el rol que pide la página.</summary>
    public IActionResult AccesoDenegado() => View();
}