# Inicio de sesión con Keycloak

Cómo funciona el login de SolarUY, qué se cambió en el código y qué tiene que configurar cada uno
para que le ande.

## Qué hace

- El **ABM** (Parques, Nodos y Paneles) y **Diagnóstico** solo se pueden usar con el rol
  `Administrador`.
- El **inicio** y el endpoint `/health` quedan abiertos.
- La web **no guarda usuarios ni contraseñas**: de eso se encarga Keycloak, que está en
  `https://auth.fabriq.uy/realms/SolarUy`. La web solo manda al usuario a loguearse allá y después
  confía en lo que Keycloak le responde.

## Cómo funciona

```
 Navegador                  SolarUY.Web                         Keycloak
     │  1. entra a /Parques     │                                   │
     │ ───────────────────────▶ │                                   │
     │  2. "no tenés sesión,    │                                   │
     │      andá a Keycloak"    │                                   │
     │ ◀─────────────────────── │                                   │
     │  3. usuario y contraseña                                     │
     │ ───────────────────────────────────────────────────────────▶ │
     │  4. "volvé a /signin-oidc con este código"                   │
     │ ◀─────────────────────────────────────────────────────────── │
     │  5. /signin-oidc?code=…  │                                   │
     │ ───────────────────────▶ │  6. canjea el código (con el      │
     │                          │     secret) por los datos y roles │
     │                          │ ────────────────────────────────▶ │
     │                          │ ◀──────────────────────────────── │
     │  7. cookie de sesión y   │                                   │
     │     redirige a /Parques  │                                   │
     │ ◀─────────────────────── │                                   │
```

- Keycloak nunca se conecta a nuestra compu: los pasos 3 y 4 los hace **el navegador**, y en el 6
  es **la web** la que llama a Keycloak. Por eso se puede probar en `localhost` aunque Keycloak
  esté en internet.
- Keycloak solo devuelve al usuario a direcciones autorizadas en el cliente `solaruy-web`
  (paso 4). Si entrás desde una dirección nueva, hay que agregarla en Keycloak (ver
  [Problemas típicos](#problemas-típicos)).
- Después del paso 7 la web reconoce al usuario por la **cookie** y no vuelve a Keycloak en cada
  pedido.
- Si el usuario está logueado pero no tiene el rol, ve la página **"No tenés permiso"**.

## Qué se cambió en el código

Todo está en `SolarUY.Web`, porque el login es responsabilidad de la presentación. Domain,
Application e Infrastructure no saben nada de usuarios.

| Archivo | Qué hace |
|---|---|
| `SolarUY.Web.csproj` | Paquete `Microsoft.AspNetCore.Authentication.OpenIdConnect` y el `UserSecretsId` |
| `Program.cs` | Configura la autenticación: **cookie** para la sesión y **OpenID Connect** para pedir el login a Keycloak. Lee los roles del claim `roles`. Agrega `app.UseAuthentication()` antes de `app.UseAuthorization()` |
| `appsettings.Development.json` | Sección `Keycloak` con `Authority` (URL del realm) y `ClientId`. **El secret no va acá** |
| `Controllers/CuentaController.cs` | `IniciarSesion`, `CerrarSesion` (cierra la sesión en la web y en Keycloak) y `AccesoDenegado` |
| `Views/Cuenta/AccesoDenegado.cshtml` | La página "No tenés permiso" |
| `Views/Shared/_Layout.cshtml` | En el menú: "Iniciar sesión", o "Hola, usuario" y "Cerrar sesión" |
| `ParquesController`, `NodosController`, `PanelesController`, `DiagnosticoController` | `[Authorize(Roles = "Administrador")]` sobre la clase: protege todas sus acciones |
| `docker-compose.yml` y `.env.example` | Variable `Keycloak__ClientSecret` para las réplicas en Docker |

## Qué tiene que configurar cada uno

La URL del realm y el client ID ya vienen en el repo. Lo único que falta es el **client secret**,
que es la contraseña de la web ante Keycloak y **nunca se sube al repo**. Pedíselo a Mariano.

### Con `dotnet run` (`http://localhost:5192`)

```bash
cd src/SolarUY.Web
dotnet user-secrets set "Keycloak:ClientSecret" "el-secret"
dotnet user-secrets list
```

El secret queda guardado en tu compu, fuera del proyecto, y la web lo lee al arrancar.

### Con Docker (`http://localhost:8080`, entrando por nginx)

Las réplicas en Docker no ven los user secrets de tu compu, así que lo leen del `.env`:

```
KEYCLOAK_CLIENT_SECRET=el-secret
```

`.env` está en el `.gitignore`. `.env.example` es solo el modelo y lleva el valor vacío.

## Usuarios

| Usuario | Para qué |
|---|---|
| `admin@solar.uy` | Probar la web con el rol `Administrador`: entra a todo |
| `consulta@solar.uy` | Probar la web con el rol `Consulta`: ve el inicio, y en el ABM le aparece "No tenés permiso" |
| `guille@`, `gonzalo@`, `fabricio@solar.uy` | **Administradores del realm** en la consola de Keycloak (`https://auth.fabriq.uy/admin/SolarUy/console`): crear usuarios, asignar roles, ver la configuración del cliente. No son para usar la web |

Las contraseñas las pasa Mariano. No se escriben en el repo.

## Cómo probar que anda

| Probás | Tiene que pasar |
|---|---|
| Entrar al inicio sin sesión | Carga, y arriba a la derecha dice "Iniciar sesión" |
| Tocar Parques sin sesión | Te manda a la página de login de Keycloak |
| Loguearte con `consulta@solar.uy` y tocar Parques | "No tenés permiso" |
| Loguearte con `admin@solar.uy` y tocar Parques | Abre normal |
| Cerrar sesión y volver a "Iniciar sesión" | Keycloak te vuelve a pedir la contraseña |

## Estado

- ✅ **`dotnet run`**: probado, anda con los dos usuarios y con el cierre de sesión.
- ⏳ **Docker con nginx**: falta probarlo. Para que ande hay que:
  1. Autorizar `http://localhost:8080/*` (y después el dominio del despliegue) en el cliente
     `solaruy-web`, en *Valid redirect URIs* y *Valid post logout redirect URIs*.
  2. Probablemente ajustar `nginx/nginx.conf`: usar `$http_host` en lugar de `$host` en `Host` y
     `X-Forwarded-Host`, para no perder el puerto, y agrandar `proxy_buffer_size`, porque la
     respuesta del login trae cookies grandes. Coordinarlo con Mariano.

## Problemas típicos

| Ves | Qué pasa | Qué hacer |
|---|---|---|
| `ArgumentException: The value cannot be an empty string (ClientId)` en cualquier página | Falta `Keycloak:ClientId` o `Keycloak:Authority` | Fijate que `appsettings.Development.json` tenga la sección `Keycloak` completa |
| Keycloak dice **"Invalid parameter: redirect_uri"** | La dirección desde la que entrás no está autorizada | Agregarla en Keycloak: Clients → `solaruy-web` → *Valid redirect URIs* |
| Error al volver de Keycloak con *invalid_client* o *unauthorized_client* | Falta el secret o está mal | Revisá `dotnet user-secrets list` o el `KEYCLOAK_CLIENT_SECRET` del `.env` |
| Siempre "No tenés permiso", incluso con el admin | La web no está recibiendo los roles | En Keycloak, el cliente tiene que tener el mapper `roles` (User Realm Role, claim `roles`, multivalued, en ID token y userinfo), y el usuario el rol `Administrador`, escrito igual |
| 502 en nginx justo después del login | Las cookies del login no entran en el buffer de nginx | Agrandar `proxy_buffer_size` (ver Estado) |
