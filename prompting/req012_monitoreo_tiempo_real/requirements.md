# Requerimiento 012: Monitoreo en tiempo real

> **Documentado antes de implementar** (SDD). Origen: requerimiento funcional de la letra («Monitoreo en tiempo real de generación de corriente y estado de los paneles») y comentario del docente en la devolución de la entrega 2: «en la consulta de lecturas, podría estar interesante que la información se actualice sola a medida que se generan nuevas lecturas».

## Descripción
Como **responsable del parque**, quiero ver en todo momento cuánta energía está generando el parque, cada nodo y cada panel, y el estado de salud de cada panel, **sin tener que recargar la página**, para reaccionar rápido cuando un panel falla.

## Criterios de Aceptación

### Pantalla de monitoreo
1. Hay una pantalla **Monitoreo** en el menú principal, que reemplaza la página de inicio por defecto de la plantilla.
2. Muestra la **potencia que se está generando ahora** (suma de la última lectura de cada panel activo):
   * del parque (total);
   * de cada nodo;
   * de cada panel, con su **estado de salud** (`OPERATIVO`, `RENDIMIENTO_BAJO`, `FALLA_DETECTADA`, `DESCONECTADO`) con el mismo color que en el resto del sistema.
3. Si hay más de un parque, se puede elegir cuál ver.
4. Los valores **se actualizan solos** cada vez que el colector guarda una nueva ronda de lecturas (cada 5 s), sin recargar la página.
5. Se ve la **hora de la última actualización**.

### Detalle del panel
6. En el detalle de un panel, la **última lectura** y la tabla de **últimas lecturas** se actualizan solas cuando llega una lectura nueva de ese panel: la nueva aparece arriba y la más vieja sale, manteniendo la misma cantidad de filas.

### Fallas
7. Cuando un panel **pasa** a `FALLA_DETECTADA` (antes no lo estaba), la pantalla de monitoreo lo **resalta** y muestra un aviso breve con el panel y el nodo. No se repite el aviso en cada ronda mientras siga en falla.

### Conexión
8. Si se pierde la conexión en vivo (por ejemplo, porque se cayó `web1` y nginx pasó a `web2`), la pantalla **se reconecta sola** y lo indica con un indicador discreto (conectado / reconectando). Mientras está desconectada, los datos que se muestran quedan marcados como no actualizados.
9. Al abrir la pantalla se ven los datos actuales enseguida, sin esperar a la próxima ronda.

### Seguridad y usabilidad
10. Ven el monitoreo y reciben datos en vivo los roles **`Administrador` y `Consulta`** (mirar no modifica nada). Sin sesión, se pide iniciar sesión. El detalle del panel sigue siendo solo de `Administrador`, como hoy.
11. Siguiendo la devolución del docente, **sin textos explicativos fijos en pantalla**: la información de ayuda va en la documentación.

## Fuera de alcance
* Gráficas históricas (eso es `req010`).
* Notificaciones fuera de la web (mail, celular).
* Balanceo con las dos réplicas activas a la vez (seguimos en activo/pasivo, así que no hace falta backplane de SignalR).
