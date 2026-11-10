# Tareas de Implementación: Monitoreo en tiempo real

> Rama `guille-signalr`, creada desde `main` **después** del merge de `req011`. Un commit por tarea (`req012 N.M: …`). Después de cada bloque: `dotnet build` y `dotnet test`.

- [ ] **0. Preparación**
  - [ ] 0.1 Commit de `requirements.md`, `specs.md` y `tasks.md` (antes de cualquier código).

- [ ] **1. Domain e Infrastructure**
  - [ ] 1.1 `ITelemetriaRepository.ObtenerDesdeAsync(desdeUtc)`.
  - [ ] 1.2 `TelemetriaRepository.ObtenerDesdeAsync` (`AsNoTracking`, sin migración).

- [ ] **2. Application**
  - [ ] 2.1 DTOs: `MonitoreoPanelDto`, `MonitoreoNodoDto`, `MonitoreoParqueDto`, `InstantaneaMonitoreoDto`, `AlertaFallaDto`.
  - [ ] 2.2 `MonitoreoService` con `ArmarInstantanea` pura y `ObtenerInstantaneaAsync`.
  - [ ] 2.3 `SeguimientoMonitoreo.Comparar` (lecturas nuevas y paso a falla).
  - [ ] 2.4 Proyecto `tests/SolarUY.Application.Tests` y tests de 2.2 y 2.3 (sección 7 de specs).

- [ ] **3. Web: servidor**
  - [ ] 3.1 `MonitoreoHub` con `[Authorize]` y los métodos de suscripción.
  - [ ] 3.2 `MonitoreoTiempoRealWorker` y opciones `Monitoreo:*`.
  - [ ] 3.3 Registro en `Program.cs` (`AddSignalR`, servicios, worker, `MapHub`).

- [ ] **4. Web: pantallas**
  - [ ] 4.1 `signalr.min.js` 8.0.7 en `wwwroot/lib/microsoft-signalr`.
  - [ ] 4.2 `MonitoreoController`, `MonitoreoIndexViewModel` y `Views/Monitoreo/Index.cshtml` con la foto inicial.
  - [ ] 4.3 `monitoreo.js`: actualización en vivo, indicador de conexión, reconexión y toast de falla.
  - [ ] 4.4 `HomeController.Index` redirige a Monitoreo; enlace en el menú.
  - [ ] 4.5 Detalle del panel: marcas en la vista y `panel-detalle.js`.

- [ ] **5. nginx**
  - [ ] 5.1 `location /hubs/` con WebSocket y `proxy_read_timeout 3600s`.

- [ ] **6. Verificación**
  - [ ] 6.1 `dotnet build` y `dotnet test` en verde.
  - [ ] 6.2 `dotnet run`: Monitoreo se actualiza cada 5 s sin recargar; el detalle de un panel agrega lecturas solas.
  - [ ] 6.3 Forzar una falla en el simulador: aparece el aviso una sola vez y la fila queda resaltada.
  - [ ] 6.4 Usuario `consulta` ve Monitoreo; sin sesión, pide login.
  - [ ] 6.5 Docker (`localhost:8080`): funciona a través de nginx (en DevTools, conexión WebSocket con estado 101).
  - [ ] 6.6 Failover: `docker stop solaruy-web1` → el indicador pasa a "reconectando" y vuelve a "conectado" con `web2`, sin recargar.
  - [ ] 6.7 `verificar-calidad` (incluye SonarQube, por cerrar el requerimiento).

- [ ] **7. Documentación**
  - [ ] 7.1 Sección de Monitoreo en la documentación (lo que no va en pantalla) y tilde en "Próximos pasos" del `README.md`.

## Uso de IA
*(Completar al terminar: qué generó la IA, qué se corrigió y qué problemas aparecieron.)*
