# Especificación Técnica: Alta disponibilidad (activo/pasivo)

> Documentado después de implementar, a partir del código de `main`.

## 1. nginx (`nginx/nginx.conf`)
* `upstream solaruy_web`:
  * `server web1:8080 max_fails=3 fail_timeout=10s;`
  * `server web2:8080 backup max_fails=3 fail_timeout=10s;`
  * `zone solaruy_web 64k;` (el contador de fallos se comparte entre los workers de nginx; sin esto el failover era aleatorio).
  * `keepalive 16;`
* `location /`: `proxy_connect_timeout 1s`, `proxy_next_upstream error timeout`, `proxy_next_upstream_tries 2`, `proxy_next_upstream_timeout 5s` (reintenta en la otra réplica).
* Cabeceras `X-Forwarded-For/Proto/Host` (con `$http_host` para conservar el puerto, ajuste de `req002`) y `X-Upstream-Addr` en la respuesta, para ver qué réplica atendió.
* `location = /nginx-health` para el propio nginx.

## 2. Web (`src/SolarUY.Web/Program.cs`)
* **Data Protection compartida:** `AddDataProtection().SetApplicationName("SolarUY.Web")` y las claves en `DataProtection:Directory` (volumen `solaruy-data-protection` montado en las dos réplicas). Así las dos pueden leer la cookie de sesión, los tokens antiforgery y el `TempData`.
* **`UseForwardedHeaders`** con `XForwardedFor | XForwardedProto | XForwardedHost`.
* **`--migrate-only`:** si la web arranca con ese argumento, aplica las migraciones, crea el anillo de claves y termina.
* **`/health`** (`AllowAnonymous`) devuelve `{ status, machine }`.

## 3. Docker (`docker-compose.yml`)
* Bloque común `x-web` para `web1` y `web2` (misma imagen y configuración), con `healthcheck` contra `/health`.
* Servicio `migracion` (perfil `migracion`) que corre la web con `--migrate-only`.
* Servicio `collector` con `container_name` fijo, para que no se pueda escalar a dos (regla "un solo colector").
* `.env.example` versionado; el `.env` real no se sube.

## 4. Reglas que impone a todo el código (ver `.agents/rules/1_clean_architecture.md`)
* Nada de estado en memoria entre pedidos (`static`, `Session` en memoria).
* Migraciones compatibles con la versión anterior (durante una actualización conviven las dos).
* Si se pasa a balanceo activo/activo, SignalR va a necesitar backplane.

## 5. Trazabilidad

| Criterio | Dónde se cumple |
|---|---|
| 1, 2 | `upstream solaruy_web` (`backup`) + `docker-compose.yml` |
| 3 | `max_fails` / `fail_timeout` + `proxy_next_upstream` + `zone` |
| 4 | Data Protection en volumen compartido |
| 5 | `UseForwardedHeaders` + cabeceras de nginx |
| 6 | `--migrate-only` + servicio `migracion` |
| 7 | `MapGet("/health")` + `healthcheck` |
| 8 | `Documentacion/actualizacion-sin-cortes.md` |
