# Requerimiento 008: Órdenes al hardware desde la interfaz

> **Documentado después de implementar.** El código ya estaba en `main` (iteración 1, commit `Acciones desde la UI: activar/desactivar panel y volcado a red`).

## Descripción
Como **operador de la planta**, quiero **activar o desactivar un panel** desde la pantalla de detalle del panel y **activar o desactivar el volcado a la red** desde la pantalla de detalle del nodo, para corregir el estado del hardware sin ir a la planta, y que el sistema lo refleje solo en la próxima lectura.

## Criterios de Aceptación
1. En el detalle de un panel hay dos botones, **Activar** y **Desactivar**, que reflejan el estado actual (`PanelSolar.Activo`): se habilita solo el que cambia el estado. Ambos están siempre visibles, tenga o no sensor el nodo.
2. En el detalle de un nodo, la fila **Volcado a red** muestra el estado (`Inversor.VolcadoRedActivo`) y, **solo si el nodo tiene sensor ARM registrado**, los botones **Activar** y **Desactivar** (se habilita solo el que cambia el estado).
3. Las cuatro acciones son `POST` con token antiforgery y solo las puede ejecutar el rol `Administrador` (la clase entera está con `[Authorize(Roles = "Administrador")]`, de `req002`).
4. La orden viaja al sensor por HTTP, como `POST` a su `UrlBase` con el identificador de hardware: `/sensores/{hw}/paneles/{codigo}/activar`, `/sensores/{hw}/paneles/{codigo}/desactivar`, `/sensores/{hw}/volcado/activar` y `/sensores/{hw}/volcado/desactivar`. El identificador y el código de panel van escapados en la ruta.
5. El resultado se interpreta por el código HTTP y se le informa al usuario:
   * `200` → mensaje de éxito ("se mandó la orden… el colector lo va a reflejar en la próxima ronda");
   * `409` → el estado ya estaba aplicado, se informa como éxito ("ya estaba activo/inactivo");
   * `404` → el simulador no reconoce el panel (o el sensor): se informa el error, no es una excepción;
   * cualquier otro código, timeout (3 s) o error de red → "el sensor no respondió a la orden".
6. **El sistema no cambia el estado en la base al mandar la orden.** `PanelSolar.Activo` e `Inversor.VolcadoRedActivo` los actualiza el colector en la próxima ronda, con lo que informa el sensor (`MarcarActividad` y `EstablecerVolcadoRed`, de `req005`).
7. Si el panel o el nodo no existen (id inválido), la acción responde `404` sin mandar nada.
8. **Nodo sin sensor:** la orden no se manda y el usuario ve "… no tiene sensor ARM registrado: no hay a quién mandarle la orden".
9. **Sensor que no responde:** el usuario ve el error y la pantalla sigue funcionando; el pedido redirige al detalle con el mensaje, nunca deja una página de error.
10. Una orden rechazada no rompe el estado del sistema: como el estado local lo fija el colector, lo que se ve en pantalla sigue siendo lo último que informó el hardware.
