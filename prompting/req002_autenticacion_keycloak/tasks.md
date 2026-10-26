# Tareas de Implementación: Inicio de sesión y roles con Keycloak

> Rama: `guille-keycloak`. Todas las tareas están hechas; queda marcado lo pendiente de decisión del equipo.

- [x] **1. Keycloak** *(configuración externa, con Mariano)*
  - [x] 1.1 Realm `SolarUy` y cliente confidencial `solaruy-web`.
  - [x] 1.2 Roles del realm `Administrador` y `Consulta`, y usuarios de prueba (`admin@solar.uy`, `consulta@solar.uy`).
  - [x] 1.3 Mapper User Realm Role → claim `roles` (multivalued, en ID token y userinfo).
  - [x] 1.4 Redirect URIs y post logout redirect URIs para `localhost:5192`, `localhost:8080` y el despliegue.

- [x] **2. Configuración de la web**
  - [x] 2.1 Paquete `Microsoft.AspNetCore.Authentication.OpenIdConnect`.
  - [x] 2.2 Sección `Keycloak` (`Authority`, `ClientId`) en `appsettings.Development.json`.
  - [x] 2.3 `ClientSecret` con `dotnet user-secrets` (`UserSecretsId` en el `.csproj`).
  - [x] 2.4 `AddAuthentication` con cookie + OpenID Connect en `Program.cs`, y mapeo de nombre y roles.
  - [x] 2.5 `app.UseAuthentication()` antes de `app.UseAuthorization()`.

- [x] **3. Presentación**
  - [x] 3.1 `CuentaController`: `IniciarSesion`, `CerrarSesion` y `AccesoDenegado`.
  - [x] 3.2 Vista `Views/Cuenta/AccesoDenegado.cshtml`.
  - [x] 3.3 Iniciar y cerrar sesión en `_Layout.cshtml`.
  - [x] 3.4 `[Authorize(Roles = "Administrador")]` en `ParquesController`, `NodosController` y `PanelesController` (resuelve los `TODO` de la etapa de seguridad).
  - [x] 3.5 `[Authorize(Roles = "Administrador")]` en `DiagnosticoController` (las réplicas en Docker corren en `Development`, así que la pantalla está publicada).

- [x] **4. Docker y alta disponibilidad**
  - [x] 4.1 `Keycloak__ClientSecret` en el bloque `x-web` de `docker-compose.yml`, desde `KEYCLOAK_CLIENT_SECRET`.
  - [x] 4.2 `KEYCLOAK_CLIENT_SECRET=` (vacío) en `.env.example`.
  - [x] 4.3 `nginx.conf`: `$http_host` en `Host` y `X-Forwarded-Host`.
  - [x] 4.4 `nginx.conf`: buffers de respuesta y `large_client_header_buffers` para las cookies del login.

- [x] **5. Verificación**
  - [x] 5.1 `dotnet clean`, `dotnet build` y `dotnet test` en verde.
  - [x] 5.2 `dotnet run` (`localhost:5192`): sin sesión → Keycloak; `consulta` → "No tenés permiso"; `admin` → entra.
  - [x] 5.3 Cerrar sesión: Keycloak vuelve a pedir la contraseña.
  - [x] 5.4 Docker (`localhost:8080`): los mismos casos, entrando por nginx.
  - [x] 5.5 Failover: `docker stop solaruy-web1`, recargar → atiende `web2` y la sesión sigue.
  - [x] 5.6 `git grep` del secret: no aparece en ningún archivo versionado.

- [x] **6. Documentación**
  - [x] 6.1 `Documentacion/keycloak.md` (cómo funciona, configuración, usuarios y problemas típicos).
  - [x] 6.2 Sección en el `README.md` y tarea marcada en "Próximos pasos".

- [ ] **7. Pendiente de decisión del equipo**
  - [ ] 7.1 Qué ve el rol `Consulta` (ver "Preguntas abiertas" en `specs.md`).
  - [ ] 7.2 Si las réplicas pasan a `Production`, mover `Authority` y `ClientId` a variables de entorno.

## Uso de IA
- **Código y configuración:** escritos paso a paso con asistencia de IA, revisados y probados por la responsable en cada paso (compilación, prueba manual y commit por paso).
- **Problemas encontrados y resueltos durante la implementación:**
  - Faltaba el `using Microsoft.AspNetCore.Authentication` para `MapJsonKey` (error de compilación).
  - Con `ClientId` vacío, toda la web fallaba, no solo las pantallas protegidas: el middleware valida las opciones en cada pedido.
  - Detrás de nginx se perdía el puerto (`$host`) y las cookies del login superaban los buffers por defecto (400 y 502).
- **Este requerimiento, `specs.md` y `tasks.md`:** armados con asistencia de IA a partir del trabajo hecho, y revisados por la responsable del módulo.
