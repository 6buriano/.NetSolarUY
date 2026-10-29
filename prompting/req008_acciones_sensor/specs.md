# Especificación Técnica: Órdenes al hardware desde la interfaz

> Documentado después de implementar, a partir del código de `main`.

## Preguntas abiertas
* La orden **no** mira `SensorDispositivo.Activo`: se le manda igual a un sensor desactivado en el sistema (el colector, en cambio, no lo consulta). ¿Se bloquea la orden con un aviso, o se deja porque el hardware puede seguir respondiendo?
* El detalle del panel muestra los botones aunque el nodo no tenga sensor (el error aparece recién después del `POST`); el detalle del nodo los esconde si no hay sensor. ¿Se unifican los dos criterios?
* `SolarUY.SensorSimulator/docs/7_simulador_sensor.md` (sección 8) describe que, al recibir `200`, el sistema actualiza `PanelSolar.Activo` / `Inversor.VolcadoRedActivo` y notifica por SignalR. El código no lo hace: el estado lo fija el colector en la ronda siguiente y SignalR no existe en la solución. ¿Se corrige el documento del simulador o se implementa esa parte?
* No hay tests automáticos de las órdenes (ni del controlador ni de `SensorHttpClient`). ¿Se agregan, o queda cubierto solo a mano?

## Resumen
Dos acciones nuevas sobre pantallas que ya existían: activar o desactivar un panel (detalle de panel) y activar o desactivar el volcado a la red del inversor (detalle de nodo). El módulo es **Presentation** (`SolarUY.Web`): reutiliza `ISensorClient` de `SolarUY.Application` y su implementación HTTP de `SolarUY.Infrastructure`, que ya tenían los cuatro métodos. No agrega entidades, tablas ni migraciones.

## 1. Capa Domain (`SolarUY.Domain`)
* **Sin cambios.** La orden no es una regla de negocio: la aplica el hardware y el dominio se entera en la próxima ronda del colector.
* Se reutilizan, tal como están desde `req005`:
  * `PanelSolar.Activo` y `PanelSolar.MarcarActividad(bool)`;
  * `Inversor.VolcadoRedActivo` e `Inversor.EstablecerVolcadoRed(bool)`.
* Invariante que deja el diseño: **el estado local lo escribe solo el colector**. Ningún controlador llama a `MarcarActividad` ni a `EstablecerVolcadoRed`.

## 2. Capa Application (`SolarUY.Application`)
* **`ResultadoOrdenSensor`** (`Sensors/ResultadoOrdenSensor.cs`, `enum`), un valor por desenlace posible del contrato del sensor:
  * `Ok` — `200`: la orden se aplicó;
  * `NoEncontrado` — `404`: no existe el sensor con ese hardware o no existe ese panel dentro del sensor (el contrato usa el mismo código);
  * `YaAplicado` — `409`: el estado pedido ya estaba aplicado;
  * `SinRespuesta` — el sensor no respondió, tardó demasiado o devolvió algo inesperado.
* **`ISensorClient`** (`Sensors/ISensorClient.cs`), cuatro métodos nuevos junto al `ObtenerEstadoAsync` que ya existía:
  * `Task<ResultadoOrdenSensor> ActivarPanelAsync(string urlBase, string identificadorHardwareARM, string codigoPanel, CancellationToken cancellationToken = default)`;
  * `Task<ResultadoOrdenSensor> DesactivarPanelAsync(...)` con los mismos parámetros;
  * `Task<ResultadoOrdenSensor> ActivarVolcadoAsync(string urlBase, string identificadorHardwareARM, CancellationToken cancellationToken = default)`;
  * `Task<ResultadoOrdenSensor> DesactivarVolcadoAsync(...)` con los mismos parámetros.
* **Sin servicio nuevo:** el controlador llama a `ISensorClient` directamente. La lógica propia es solo elegir el método y armar el mensaje; no hay regla de negocio ni consulta a la base que justifique una clase `OrdenSensorService`. (Mismo criterio que `ObtenerEstadoAsync`, que también usa el colector sin servicio intermedio.)

## 3. Capa Infrastructure (`SolarUY.Infrastructure`)
* **`SensorHttpClient`** (`Sensors/SensorHttpClient.cs`) implementa los cuatro métodos. Cada uno arma la URL y delega en un helper privado:
  * `EnviarOrdenAsync(string url, string identificadorHardwareARM, CancellationToken)` → `ResultadoOrdenSensor`.
* **Reglas del helper:**
  1. `POST` **sin cuerpo** (`PostAsync(url, content: null)`).
  2. Traduce el código HTTP: `200` → `Ok`; `404` → `NoEncontrado`; `409` → `YaAplicado`; cualquier otro → `SinRespuesta` (con `LogWarning` del código).
  3. `catch` de `OperationCanceledException` **solo** cuando la cancelación no vino de afuera (timeout del `HttpClient`) → `SinRespuesta`. Si la cancelación es del pedido, la excepción sube.
  4. `catch` de `HttpRequestException` → `SinRespuesta` con `LogWarning`. Ningún método tira excepción por un sensor caído.
* **URL:** `ArmarUrl(urlBase, identificadorHardwareARM, params string[] segmentos)` arma `{urlBase}/sensores/{hw}/...` y **escapa cada segmento** con `Uri.EscapeDataString`, porque el hardware y el código de panel viajan en la ruta. Rutas resultantes:
  * `POST /sensores/{hw}/paneles/{codigo}/activar`
  * `POST /sensores/{hw}/paneles/{codigo}/desactivar`
  * `POST /sensores/{hw}/volcado/activar`
  * `POST /sensores/{hw}/volcado/desactivar`
* **Timeout:** el mismo cliente HTTP ya registrado en `DependencyInjection.AddInfrastructure` (`AddHttpClient<ISensorClient, SensorHttpClient>`) con `TimeoutSensor` = 3 s. No hay registro nuevo.
* **Sin migración:** no se toca el modelo de datos.

## 4. Capa Presentation (MVC)
### `PanelesController`
* Ya inyecta `ISensorClient` (se agregó al constructor en esta funcionalidad).
* `[HttpPost] [ValidateAntiForgeryToken] Activar(Guid id, CancellationToken)` → `EnviarOrdenPanelAsync(id, activar: true, ...)`.
* `[HttpPost] [ValidateAntiForgeryToken] Desactivar(Guid id, CancellationToken)` → `EnviarOrdenPanelAsync(id, activar: false, ...)`.
* **`EnviarOrdenPanelAsync(id, activar, ct)`**, pasos:
  1. `_panelService.ObtenerPorIdAsync(id)`: si es `null` → `NotFound()`.
  2. `_nodoService.ObtenerPorIdAsync(panel.NodoId)` → `nodo?.Inversor.Sensor`.
  3. Si el sensor es `null` → `TempData["MensajeError"]` con el aviso de nodo sin sensor y `RedirectToAction(Detalle, { id })`. **No se manda nada.**
  4. Llama a `ActivarPanelAsync` o `DesactivarPanelAsync` con `sensor.UrlBase`, `sensor.IdentificadorHardwareARM` y `panel.CodigoPanel`.
  5. Elige la clave (`MensajeExito` para `Ok`/`YaAplicado`, `MensajeError` para el resto) y el texto (tabla de mensajes).
  6. `RedirectToAction(nameof(Detalle), new { id })` siempre: patrón POST-redirect-GET.
* No hay acción `GET` de confirmación: la orden se manda con un solo clic.

### `NodosController`
* Ya inyecta `ISensorClient`.
* `[HttpPost] [ValidateAntiForgeryToken] ActivarVolcado(Guid id, CancellationToken)` → `EnviarOrdenVolcadoAsync(id, activar: true, ...)`.
* `[HttpPost] [ValidateAntiForgeryToken] DesactivarVolcado(Guid id, CancellationToken)` → `EnviarOrdenVolcadoAsync(id, activar: false, ...)`.
* **`EnviarOrdenVolcadoAsync(id, activar, ct)`**: mismos pasos que el helper de paneles, pero sin el paso del panel; si el nodo no existe → `NotFound()`, si no tiene sensor → mensaje de error y redirect.

### Mensajes al usuario
| Resultado | Clave de `TempData` | Texto (panel / volcado) |
|---|---|---|
| `Ok` | `MensajeExito` | "Se mandó la orden de activar/desactivar el panel. El colector lo va a reflejar en la próxima ronda." / ídem con "el volcado a la red" |
| `YaAplicado` | `MensajeExito` | "El panel ya estaba activo/inactivo." / "El volcado a la red ya estaba activo/inactivo." |
| `NoEncontrado` | `MensajeError` | "El simulador no reconoce este panel: puede que se haya quitado del hardware." / "El simulador no reconoce este sensor: …" |
| `SinRespuesta` | `MensajeError` | "El sensor no respondió a la orden. Probá de nuevo en un momento." |
| Nodo sin sensor | `MensajeError` | "El nodo de este panel no tiene sensor ARM registrado: no hay a quién mandarle la orden." / "Este nodo no tiene sensor ARM registrado: …" |

Los dos mensajes los muestra `_Layout.cshtml` como alerta, así que sobreviven al redirect.

### Vistas
* `Views/Paneles/Detalle.cshtml`, en la fila **Habilitado en el hardware**: badge Activo/Inactivo y los dos `<form method="post">` (uno por acción) con el botón opuesto `disabled`. Los botones se muestran siempre.
* `Views/Nodos/Detalle.cshtml`, en la fila **Volcado a red**: badge Activo/Inactivo y los dos formularios **dentro de un `@if (Model.Nodo.Inversor.Sensor is not null)`**, con el botón opuesto `disabled`.
* No hay ViewModels nuevos: se usan los que ya alimentaban las dos pantallas (`PanelDetalleViewModel`, `NodoDetalleViewModel`).

## Trazabilidad

| Criterio | Dónde se cumple |
|---|---|
| 1 | `Paneles/Detalle.cshtml` (dos formularios con `disabled` según `Activo`) + `PanelesController.Activar/Desactivar` |
| 2 | `Nodos/Detalle.cshtml` (`@if` sobre `Inversor.Sensor`) + `NodosController.ActivarVolcado/DesactivarVolcado` |
| 3 | `[Authorize(Roles = "Administrador")]` en las dos clases + `[HttpPost] [ValidateAntiForgeryToken]` en las cuatro acciones |
| 4 | `SensorHttpClient` (`ActivarPanelAsync`, `DesactivarPanelAsync`, `ActivarVolcadoAsync`, `DesactivarVolcadoAsync`) + `ArmarUrl` |
| 5 | `EnviarOrdenAsync` (mapeo 200/404/409/otros) + `ResultadoOrdenSensor` + `switch` de mensajes en los dos controladores |
| 6 | Los controladores no tocan el dominio; `ColectorTelemetriaWorker` aplica `MarcarActividad` y `EstablecerVolcadoRed` (req005) |
| 7 | `ObtenerPorIdAsync` → `null` → `NotFound()` en los dos helpers |
| 8 | `nodo?.Inversor.Sensor is null` → `MensajeError` + redirect, sin llamar al sensor |
| 9 | `EnviarOrdenAsync` no propaga excepciones (timeout y red → `SinRespuesta`) y el helper siempre redirige al detalle |
| 10 | `EnviarOrdenAsync` no escribe en la base: el estado mostrado sigue siendo el que guardó el colector |
