# Tareas de Implementación: Alta disponibilidad (activo/pasivo)

> Documentado después de implementar: las tareas reflejan lo que ya está en `main` (PR #2).

- [x] **1. nginx**
  - [x] 1.1 `upstream` con `web1` principal y `web2` `backup`.
  - [x] 1.2 `zone` compartida y `proxy_connect_timeout 1s` para un failover rápido y estable.
  - [x] 1.3 Reintento en la otra réplica (`proxy_next_upstream`) y cabecera `X-Upstream-Addr`.

- [x] **2. Web**
  - [x] 2.1 Data Protection con claves en un directorio compartido.
  - [x] 2.2 `UseForwardedHeaders`.
  - [x] 2.3 Modo `--migrate-only` (migraciones + anillo de claves).
  - [x] 2.4 Endpoint `/health`.

- [x] **3. Docker**
  - [x] 3.1 `web1` y `web2` desde el bloque `x-web`, con healthcheck.
  - [x] 3.2 Servicio `migracion` con perfil propio.
  - [x] 3.3 Volumen `solaruy-data-protection`.
  - [x] 3.4 Collector con una sola instancia.
  - [x] 3.5 `.env.example` versionado.

- [x] **4. Verificación**
  - [x] 4.1 `docker stop solaruy-web1` → atiende `web2` y la sesión de Keycloak sigue (probado también en `req002`).
  - [x] 4.2 Actualización réplica por réplica sin pedidos fallidos (`actualizacion-sin-cortes.md`).

- [x] **5. Documentación**
  - [x] 5.1 `Documentacion/diagrama-infraestructura.md` y `actualizacion-sin-cortes.md`.

## Uso de IA
- **Configuración y código:** implementados en la iteración 1 con asistencia de IA, sin documentos SDD previos.
- **Este requerimiento, `specs.md` y `tasks.md`:** reconstruidos con asistencia de IA leyendo el código de `main`, y revisados por el equipo. *(Completar: quién lo revisó.)*
