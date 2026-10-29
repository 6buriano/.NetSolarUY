# Tareas de Implementación: Órdenes al hardware desde la interfaz

> Documentado después de implementar: las tareas reflejan lo que ya está en `main` (commit `Acciones desde la UI: activar/desactivar panel y volcado a red`).

- [x] **1. Domain**
  - [x] 1.1 Revisar el dominio: `PanelSolar.Activo` / `MarcarActividad(bool)` e `Inversor.VolcadoRedActivo` / `EstablecerVolcadoRed(bool)` ya alcanzan. No se crea nada nuevo.
  - [x] 1.2 Tests de esos comportamientos: `PanelSolarTests.MarcarActividad_CambiaElFlag` e `InversorTests.EstablecerVolcadoRed_CambiaElFlag` (de `req005`).

- [x] **2. Infrastructure**
  - [x] 2.1 `SensorHttpClient`: helper privado `EnviarOrdenAsync(url, identificadorHardwareARM, ct)` con `POST` sin cuerpo y el mapeo de códigos (`200` → `Ok`, `404` → `NoEncontrado`, `409` → `YaAplicado`, resto → `SinRespuesta`).
  - [x] 2.2 `SensorHttpClient.ActivarPanelAsync` y `DesactivarPanelAsync`: arman la ruta `paneles/{codigo}/activar|desactivar`.
  - [x] 2.3 `SensorHttpClient.ActivarVolcadoAsync` y `DesactivarVolcadoAsync`: arman la ruta `volcado/activar|desactivar`.
  - [x] 2.4 `ArmarUrl`: escapar cada segmento con `Uri.EscapeDataString`.
  - [x] 2.5 Tolerancia a fallas en `EnviarOrdenAsync`: timeout propio del `HttpClient` y `HttpRequestException` → `SinRespuesta`, con `LogWarning` y sin excepción.

- [x] **3. Application**
  - [x] 3.1 `Sensors/ResultadoOrdenSensor.cs`: `enum` con `Ok`, `NoEncontrado`, `YaAplicado`, `SinRespuesta` y el comentario de qué código HTTP representa cada uno.
  - [x] 3.2 `Sensors/ISensorClient.cs`: los cuatro métodos de orden con sus firmas.
  - [x] 3.3 Decidir no crear un servicio de aplicación: el controlador usa `ISensorClient` directo.

- [x] **4. Web (`Program.cs`)**
  - [x] 4.1 Confirmar el registro existente `AddHttpClient<ISensorClient, SensorHttpClient>` con `TimeoutSensor` de 3 s en `Infrastructure/DependencyInjection.cs`. No hizo falta tocarlo.
  - [x] 4.2 Confirmar que `[Authorize(Roles = "Administrador")]` ya viene de `req002`; no se agrega nada en `Program.cs`.

- [x] **5. Presentation**
  - [x] 5.1 `PanelesController`: inyectar `ISensorClient` en el constructor.
  - [x] 5.2 `PanelesController.Activar(Guid id, CancellationToken)`: `[HttpPost]`, antiforgery, delega en el helper.
  - [x] 5.3 `PanelesController.Desactivar(Guid id, CancellationToken)`: ídem con `activar: false`.
  - [x] 5.4 `PanelesController.EnviarOrdenPanelAsync`: panel → nodo → sensor; sin sensor, error y redirect; con sensor, llamar a `ActivarPanelAsync`/`DesactivarPanelAsync` con `panel.CodigoPanel`.
  - [x] 5.5 `NodosController`: inyectar `ISensorClient` en el constructor.
  - [x] 5.6 `NodosController.ActivarVolcado(Guid id, CancellationToken)` y `DesactivarVolcado(...)`: `[HttpPost]`, antiforgery, delegan en el helper.
  - [x] 5.7 `NodosController.EnviarOrdenVolcadoAsync`: nodo → sensor; sin sensor, error y redirect; con sensor, llamar al método de volcado.
  - [x] 5.8 Mensajes en los dos helpers: `ResultadoOrdenSensor` → `TempData["MensajeExito"]` o `TempData["MensajeError"]`, con redirect a `Detalle`. Los muestra `_Layout.cshtml` (de `req001`).
  - [x] 5.9 `Views/Paneles/Detalle.cshtml`: botones Activar/Desactivar en la fila "Habilitado en el hardware", uno de los dos `disabled`.
  - [x] 5.10 `Views/Nodos/Detalle.cshtml`: botones Activar/Desactivar del volcado, dentro del `@if` que exige sensor.

- [x] **6. Verificación**
  - [x] 6.1 `dotnet build` de la solución en verde (0 errores y 0 advertencias).
  - [ ] 6.2 `dotnet test` de `tests/SolarUY.Domain.Tests`: no se pudo confirmar en la máquina donde se escribió esto (el `testhost` no arranca por una restricción de permisos del entorno, no por el código). Los tests de los comportamientos del punto 1 existen.
  - [ ] 6.3 Prueba manual, criterio por criterio (ver abajo).
  - [ ] 6.4 `dotnet test` de `tests/CodeQuality.Tests`: el proyecto no existe todavía.
  - [ ] 6.5 SonarQube (`tools/SonarQube/analizar.sh`): falta integrarlo.

- [ ] **7. Pendiente**
  - [ ] 7.1 Prueba manual con el simulador: desactivar un panel, ver el `409` al repetir, y comprobar que el colector lo refleja en la ronda siguiente.
  - [ ] 7.2 Prueba manual: sensor apagado en el simulador → mensaje "no respondió" y la pantalla sigue andando.
  - [ ] 7.3 Prueba manual: nodo sin sensor → mensaje de error, sin llamada HTTP.
  - [ ] 7.4 Prueba manual: panel que el simulador no conoce → mensaje "no reconoce este panel".
  - [ ] 7.5 Responder las preguntas abiertas de `specs.md` (sensor desactivado, botones del panel sin sensor, documento del simulador, tests de las órdenes).
  - [ ] 7.6 Tests automáticos del mapeo de códigos de `EnviarOrdenAsync` y de los dos helpers de los controladores.
  - [ ] 7.7 Pasar `verificar-calidad` (ArchUnit + SonarQube) cuando estén integrados.

## Uso de IA
- **Código:** implementado con asistencia de IA, sin documentos SDD previos; el cliente del sensor ya traía los cuatro métodos desde el ABM inicial y esta funcionalidad solo los expuso en pantalla.
- **Este requerimiento, `specs.md` y `tasks.md`:** reconstruidos leyendo el código de `main` (`ISensorClient`, `ResultadoOrdenSensor`, `SensorHttpClient`, los dos controladores y las dos vistas). *(Completar: quién lo revisó.)*
