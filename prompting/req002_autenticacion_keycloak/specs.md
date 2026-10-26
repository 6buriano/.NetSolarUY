# Especificaciones Técnicas: Inicio de sesión y roles con Keycloak

> Este documento describe el diseño implementado en la rama `guille-keycloak`. Se armó con asistencia de IA a partir del trabajo hecho y lo revisó la responsable del módulo.

## Preguntas abiertas
- ¿Qué puede ver el rol `Consulta`? Propuesta: el dashboard de Monitoreo cuando exista (iteración 2) y, si se decide, el listado y el detalle del ABM en modo solo lectura. Hoy solo ve el inicio.
- Las réplicas en Docker corren con `ASPNETCORE_ENVIRONMENT=Development`. ¿Se pasan a `Production` para el despliegue? Si se hace, `Authority` y `ClientId` deben ir como variables de entorno (hoy los toman de `appsettings.Development.json`).

## Resumen
Autenticación con un proveedor externo (Keycloak, OpenID Connect, flujo *Authorization Code*) y autorización por roles en la capa de presentación. Afecta solo a `SolarUY.Web` y a la configuración de despliegue: Domain, Application e Infrastructure no cambian, porque la identidad del usuario no es parte del negocio.

## 1. Capa Domain (`SolarUY.Domain`)
Sin cambios. Las entidades no conocen usuarios ni roles.

## 2. Capa Application (`SolarUY.Application`)
Sin cambios. Los servicios no reciben el usuario: la autorización se resuelve antes de llegar a ellos.

## 3. Capa Infrastructure (`SolarUY.Infrastructure`)
Sin cambios en código ni en la base: no hay tablas de usuarios. No hay migración.

## 4. Capa Presentation (`SolarUY.Web`)

### 4.1. Paquete
- `Microsoft.AspNetCore.Authentication.OpenIdConnect` (versión 10.x, la de .NET 10).

### 4.2. Configuración (`Program.cs`)
- `AddAuthentication` con dos esquemas:
  - **Por defecto: cookie** (`CookieAuthenticationDefaults`), que guarda la sesión. `AccessDeniedPath = "/Cuenta/AccesoDenegado"`.
  - **Challenge: OpenID Connect** (`OpenIdConnectDefaults`), que manda a Keycloak cuando no hay sesión.
- Opciones de OpenID Connect:
  - `Authority`, `ClientId` y `ClientSecret` desde la sección `Keycloak` de la configuración.
  - `ResponseType = "code"`, `SaveTokens = true` (necesario para el cierre de sesión en Keycloak), `GetClaimsFromUserInfoEndpoint = true`.
  - `MapInboundClaims = false`, `NameClaimType = "preferred_username"`, `RoleClaimType = "roles"` y `ClaimActions.MapJsonKey("roles", "roles")`.
- Middleware: `app.UseAuthentication()` antes de `app.UseAuthorization()`, ambos después de `UseRouting()` y de `UseForwardedHeaders()`.

### 4.3. Configuración por entorno
| Dato | `dotnet run` | Docker |
|---|---|---|
| `Keycloak:Authority` | `appsettings.Development.json` (`https://auth.fabriq.uy/realms/SolarUy`) | Igual: las réplicas leen el mismo archivo |
| `Keycloak:ClientId` | `appsettings.Development.json` (`solaruy-web`) | Igual |
| `Keycloak:ClientSecret` | *user secrets* (`UserSecretsId` en el `.csproj`) | Variable `Keycloak__ClientSecret` desde `KEYCLOAK_CLIENT_SECRET` en `.env` |

`.env.example` lleva la variable vacía. `.env` está en `.gitignore`.

### 4.4. `CuentaController` (nuevo, `[AllowAnonymous]`)
| Acción | Verbo | Qué hace |
|---|---|---|
| `IniciarSesion(returnUrl)` | GET | `Challenge` con OpenID Connect. Solo acepta `returnUrl` locales (`Url.IsLocalUrl`); si no, vuelve al inicio |
| `CerrarSesion` | POST, `[ValidateAntiForgeryToken]` | `SignOut` de la cookie y de OpenID Connect, y vuelve al inicio |
| `AccesoDenegado` | GET | Muestra la vista "No tenés permiso" |

### 4.5. Vistas
- `Views/Cuenta/AccesoDenegado.cshtml`: mensaje con `User.Identity.Name` y enlace al inicio.
- `Views/Shared/_Layout.cshtml`: bloque a la derecha del menú. Si `User.Identity.IsAuthenticated`, muestra "Hola, *usuario*" y un formulario POST a `CerrarSesion`; si no, un enlace a `IniciarSesion`.

### 4.6. Autorización
- `[Authorize(Roles = "Administrador")]` a nivel de clase en `ParquesController`, `NodosController`, `PanelesController` y `DiagnosticoController`.
- `HomeController` y `/health` quedan abiertos (`/health` con `.AllowAnonymous()`).

### 4.7. Keycloak (configuración externa, la administra el equipo)
- Realm `SolarUy`, cliente confidencial `solaruy-web` (con secret).
- *Valid redirect URIs* y *Valid post logout redirect URIs*: `http://localhost:5192/...`, `http://localhost:8080/*` y `https://solaruy.fabriq.uy/...`.
- Mapper **User Realm Role** con claim `roles`, multivalued, en ID token, access token y userinfo.
- Roles del realm: `Administrador` y `Consulta`.

### 4.8. Balanceador (`nginx/nginx.conf`)
- `proxy_set_header Host $http_host` y `X-Forwarded-Host $http_host`: conservan el puerto para que la web arme bien la `redirect_uri` (`http://localhost:8080/signin-oidc`).
- `proxy_buffer_size 128k`, `proxy_buffers 4 256k` y `proxy_busy_buffers_size 256k`: la respuesta del login trae cookies grandes.
- `large_client_header_buffers 4 32k`: el navegador manda esas cookies en cada pedido; con el límite por defecto (8 KB) nginx responde 400.

### 4.9. Alta disponibilidad
- No requiere cambios propios: la cookie de sesión la cifra Data Protection, y las dos réplicas comparten el anillo de claves (volumen `solaruy-data-protection`, configurado en la iteración de infraestructura). Por eso la sesión sigue válida si cambia la réplica.

## Trazabilidad

| Criterio | Dónde se cumple | Estado |
|---|---|---|
| 1 Login con Keycloak | `AddOpenIdConnect` en `Program.cs` | ✅ |
| 2 Redirige al login | `DefaultChallengeScheme = OpenIdConnect` + `[Authorize]` | ✅ |
| 3 Vuelve a donde quería ir | `RedirectUri` del challenge | ✅ |
| 4 Estado de la sesión en el menú | `_Layout.cshtml` | ✅ |
| 5 Sesión persistente | Cookie (`DefaultScheme`) | ✅ |
| 6 Cierre en los dos lados | `CuentaController.CerrarSesion` (dos esquemas) + `SaveTokens` | ✅ |
| 7 Cierre solo con botón | `[HttpPost]` + `[ValidateAntiForgeryToken]` | ✅ |
| 8 ABM y Diagnóstico solo `Administrador` | `[Authorize(Roles = "Administrador")]` | ✅ |
| 9 "No tenés permiso" | `AccessDeniedPath` + `AccesoDenegado.cshtml` | ✅ |
| 10 Inicio y `/health` abiertos | Sin `[Authorize]`; `.AllowAnonymous()` | ✅ |
| 11 Roles en Keycloak | Mapper `roles` + `RoleClaimType` | ✅ |
| 12 Secret fuera del repo | User secrets / `.env` | ✅ |
| 13 Configurable sin tocar código | Sección `Keycloak` + variables de entorno | ✅ |
| 14 Solo redirecciones locales | `Url.IsLocalUrl` en `IniciarSesion` | ✅ |
| 15 Funciona detrás de nginx | Ajustes de `nginx.conf` (4.8) | ✅ |
| 16 La sesión sobrevive al failover | Data Protection compartido (4.9) | ✅ |
