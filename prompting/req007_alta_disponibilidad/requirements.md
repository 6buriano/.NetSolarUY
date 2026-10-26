# Requerimiento 007: Alta disponibilidad de la web (activo/pasivo)

> **Documentado después de implementar.** El código ya estaba en `main` (iteración 1, PR #2). No es un requerimiento de pantalla sino de despliegue.

## Descripción
Como **responsable de la plataforma**, quiero que la web siga atendiendo si se cae una instancia, y poder actualizarla sin cortar el servicio, porque el sistema monitorea la planta todo el tiempo.

## Criterios de Aceptación
1. La web corre en **dos réplicas idénticas** (`web1` y `web2`) detrás de **nginx**, que es la única entrada (puerto 8080).
2. Es **activo/pasivo**, no balanceo: `web1` atiende todo y `web2` es la reserva.
3. Si `web1` no responde, nginx pasa el pedido a `web2` sin que el usuario vea un error, y deja de mandarle tráfico a `web1` por un rato.
4. **La sesión no se pierde al cambiar de réplica**: el usuario sigue logueado (cookie de Keycloak), y los formularios y los mensajes (`TempData`) siguen funcionando.
5. La web conoce la dirección real del pedido (esquema, host y puerto) aunque esté detrás de nginx.
6. Las migraciones de la base se aplican **una sola vez**, con un paso aparte, no al arrancar cada réplica.
7. Cada réplica expone un `/health` (sin login) que dice qué réplica contestó, y Docker lo usa como healthcheck.
8. Se puede actualizar réplica por réplica sin pedidos fallidos (procedimiento en `Documentacion/actualizacion-sin-cortes.md`).
