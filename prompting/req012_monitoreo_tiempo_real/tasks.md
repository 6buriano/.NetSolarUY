# Tareas de Implementación: Monitoreo en tiempo real

> Rama `guille-signalr`, creada desde `main` (`78c2d62`). Se trajo la rama `mariano` (req011) con un merge porque req012 usa el índice `fecha_hora` que agrega esa rama y toca los mismos archivos (`Program.cs`, `_Layout.cshtml`, `ITelemetriaRepository`). Un commit por tarea (`req012 N.M: …`), compilando antes de cada commit.

- [x] **0. Preparación**
  - [x] 0.1 Commit de `requirements.md`, `specs.md` y `tasks.md` (antes de cualquier código).

- [x] **1. Domain e Infrastructure**
  - [x] 1.1 `ITelemetriaRepository.ObtenerDesdeAsync(desdeUtc)`.
  - [x] 1.2 `TelemetriaRepository.ObtenerDesdeAsync` (`AsNoTracking`, sin migración). *(Un solo commit con 1.1: la 1.1 sola no compila.)*

- [ ] **2. Application**
  - [x] 2.1 DTOs: `MonitoreoPanelDto`, `MonitoreoNodoDto`, `MonitoreoParqueDto`, `InstantaneaMonitoreoDto`, `AlertaFallaDto`.
  - [x] 2.2 `MonitoreoService` con `ArmarInstantanea` pura y `ObtenerInstantaneaAsync`.
  - [x] 2.3 `SeguimientoMonitoreo.Comparar` (lecturas nuevas y paso a falla).
  - [ ] 2.4 Proyecto `tests/SolarUY.Application.Tests` y tests de 2.2 y 2.3. **Pendiente para la entrega 3** (las dos funciones son puras justamente para poder probarlas).

- [x] **3. Web: servidor**
  - [x] 3.1 `MonitoreoHub` con `[Authorize]` y los métodos de suscripción.
  - [x] 3.2 `MonitoreoTiempoRealWorker` y opciones `Monitoreo:*`.
  - [x] 3.3 Registro en `Program.cs` (`AddSignalR` con enums como texto, servicios, worker, `MapHub`).

- [x] **4. Web: pantallas**
  - [x] 4.1 `signalr.min.js` 8.0.7 en `wwwroot/lib/microsoft-signalr`.
  - [x] 4.2 `MonitoreoController`, `MonitoreoIndexViewModel` y `Views/Monitoreo/Index.cshtml` con la foto inicial.
  - [x] 4.3 `monitoreo.js`: actualización en vivo, indicador de conexión, reconexión y toast de falla.
  - [x] 4.4 `HomeController.Index` redirige a Monitoreo; enlace en el menú.
  - [x] 4.5 Detalle del panel: marcas en la vista y `panel-detalle.js` (sin duplicar lecturas que ya vinieron con la página).

- [x] **5. nginx**
  - [x] 5.1 `location /hubs/` con WebSocket y `proxy_read_timeout 3600s`.

- [ ] **6. Verificación**
  - [x] 6.1 `dotnet build` en verde después de cada tarea.
  - [x] 6.2 `dotnet run`: Monitoreo se actualiza cada 5 s sin recargar; el detalle de un panel agrega lecturas solas.
  - [x] 6.3 Falla forzada en el simulador (`POST /simulador/fallas/ARM-001/P02?tipo=sobretemperatura`): la fila queda en rojo y el aviso aparece una sola vez (5 minutos en falla sin repetirse). Al restablecerla, el panel vuelve a `OPERATIVO`.
  - [x] 6.4 Usuario `consulta` ve Monitoreo; sin sesión, pide login.
  - [x] 6.5 Docker (`localhost:8080`): funciona a través de nginx (WebSocket `monitoreo?id=…` con estado 101).
  - [x] 6.6 Failover: `docker stop solaruy-web1` → la pantalla sigue "En vivo" atendida por `web2`, sin recargar ni volver a iniciar sesión.
  - [ ] 6.7 `verificar-calidad` (SonarQube, por cerrar el requerimiento).

- [ ] **7. Documentación**
  - [ ] 7.1 Sección de Monitoreo en la documentación y tilde en "Próximos pasos" del `README.md`.

## Uso de IA
- **Documentos SDD:** `requirements.md`, `specs.md` y este `tasks.md` se escribieron con asistencia de IA **antes** del código, a partir de la letra y de la devolución del docente, y los revisó la responsable.
- **Código:** generado con asistencia de IA tarea por tarea. La responsable creó cada archivo, compiló, probó y commiteó paso a paso, revisando la explicación de cada parte.
- **Decisión de diseño revisada:** se descartó que el Collector avise a la web (acoplaba procesos y obligaba a autenticar el aviso); el worker vive en la web y lee MySQL cada 5 s.
- **Problemas encontrados y cómo se resolvieron:**
  - Después de actualizar el SDK, el compilador de vistas Razor dio decenas de errores en vistas que nadie había tocado: era el servidor de compilación de .NET trabado. Se resolvió con `dotnet build-server shutdown` (no hubo que cambiar código).
  - En Docker los paneles no tenían lecturas: el sensor de ejemplo se había creado en modo `dotnet run` con la URL `http://localhost:5100`, que dentro de Docker no existe. Se corrigió a `http://sensor-simulator:8080` desde la edición del nodo (dato, no código).
  - En el detalle del panel una lectura aparecía dos veces cuando la página se abría justo durante una ronda del colector (venía con la página y también por SignalR). Se resolvió identificando cada fila por el `Id` de la lectura.
  - Los números actualizados en vivo salían con coma y los del servidor con punto. Se unificó el formato del JavaScript con el del servidor.
  - Las fechas de la base llegan al navegador sin la "Z" de UTC; el JavaScript la agrega antes de pasarlas a hora de Montevideo.
- **Pendiente detectado:** al abrir Monitoreo, un panel que ya estaba en falla muestra el badge rojo pero la fila no se resalta (el resaltado se activa con el aviso).