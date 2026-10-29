# Requerimiento 009: Gestión de tareas de mantenimiento

> **Documentado después de implementar.** El código ya estaba en `main` (commit `f73e917`).

## Descripción
Como **responsable de mantenimiento**, quiero programar tareas sobre cada panel (limpieza, reparación, inspección preventiva o reemplazo), con fecha, descripción y personal asignado, y seguir su ciclo de vida hasta completarlas o cancelarlas, para que el historial de cada panel muestre qué trabajo recibió y cuándo.

## Criterios de Aceptación
1. Se listan las tareas de mantenimiento, con filtro opcional **por panel** y **por estado**. Sin filtros se ven todas.
2. El listado muestra: panel, tipo, fecha programada, personal asignado y estado. Cada fila tiene un acceso al detalle.
3. Para programar una tarea son obligatorios: panel, tipo, fecha programada y descripción. El **personal asignado es opcional**.
4. El **tipo** se elige de: `LIMPIEZA`, `REPARACION`, `INSPECCION_PREVENTIVA`, `REEMPLAZO`. Por defecto se propone `INSPECCION_PREVENTIVA`.
5. La **descripción** es de texto libre, obligatoria, y no supera los 500 caracteres.
6. El **personal asignado** no supera los 150 caracteres.
7. El panel elegido tiene que existir.
8. Toda tarea nueva nace en estado **`PENDIENTE`**.
9. El ciclo de vida es `PENDIENTE` → `EN_PROCESO` → `COMPLETADO`, y desde `PENDIENTE` o `EN_PROCESO` se puede **cancelar** (`CANCELADO`).
10. **Solo una tarea `PENDIENTE` puede pasar a `EN_PROCESO`.** Cualquier otro estado se rechaza.
11. **Solo una tarea `EN_PROCESO` puede completarse.** Cualquier otro estado se rechaza.
12. **No se puede reprogramar una tarea `COMPLETADO`.** Sí se puede reprogramar una `PENDIENTE`, una `EN_PROCESO` y una `CANCELADO`.
13. **No se puede cancelar una tarea `COMPLETADO`.**
14. Reprogramar cambia la **fecha programada**, la **descripción** y el **personal asignado**. No se puede cambiar el panel ni el tipo de una tarea ya creada.
15. **Al completar una tarea se guarda la fecha de realización** (fecha y hora del momento en que se completa), y queda visible en el detalle.
16. El detalle de una tarea muestra tipo, panel, estado, fecha programada, fecha realizada, personal asignado y descripción, y ofrece **solo las acciones válidas** para su estado: `PENDIENTE` → iniciar o cancelar; `EN_PROCESO` → completar o cancelar; `COMPLETADO` o `CANCELADO` → ninguna.
17. El botón **Reprogramar** no aparece para tareas `COMPLETADO` ni `CANCELADO`.
18. El **detalle del panel** muestra su historial de mantenimiento, de la fecha programada más reciente a la más antigua, con un acceso a cada tarea y un atajo para programar una nueva sobre ese panel.
19. Cada operación exitosa muestra un mensaje de confirmación; una regla de negocio rechazada muestra el mensaje de error sin perder el formulario.
20. Al eliminar un panel se eliminan también sus tareas de mantenimiento (resuelto en `req004`).
21. Solo el rol `Administrador` accede a estas pantallas (resuelto en `req002`).
