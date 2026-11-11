# Especificación Técnica: Monitoreo en tiempo real

> Escrito **antes** de implementar. Se implementa sobre `main` **después** de mergear `req011` (usa el índice `fecha_hora` que agrega esa rama y evita conflictos en `Program.cs`, `_Layout.cshtml` y `ITelemetriaRepository`).

## Resumen
La web empuja a los navegadores, con **SignalR**, la potencia actual del parque, de cada nodo y de cada panel, el estado de salud de cada panel y las lecturas nuevas.

**Decisión de diseño:** el `Collector` **no se toca**. Un `BackgroundService` **dentro de la web** consulta MySQL cada 5 s, arma una "foto" del estado actual y la envía al hub. Alternativa descartada: que el `Collector` le avise a la web después de cada ronda, porque acopla dos procesos, obliga a autenticar ese aviso y a pasarlo por nginx.

Se eligió así por tres motivos:
- **No agrega dependencias** entre procesos.
- **Encaja con el activo/pasivo:** las dos réplicas corren el worker. La consulta es liviana (una ventana de 15 s de lecturas), y solo reciben datos los navegadores conectados a la réplica que atiende.
- **Sobrevive al failover sin configuración:** el navegador se reconecta a `web2`, que ya está mandando lo mismo.

Mientras sigamos en activo/pasivo no hace falta backplane (regla de `1_clean_architecture.md`).

## 1. Domain (`SolarUY.Domain`)
* **`ITelemetriaRepository.ObtenerDesdeAsync(DateTime desdeUtc, CancellationToken)`** → `IReadOnlyList<LecturaTelemetria>`.
  * Devuelve las lecturas de **todos** los paneles con `FechaHora >= desdeUtc`, sin tracking.
  * Siempre se llama con una ventana corta (15 s), nunca sin límite (regla de lecturas de `4_modelado_dominio.md`).
  * Usa el índice `fecha_hora` de `req011`.
* Sin cambios en entidades. El estado del panel ya lo decide `Nodo.EvaluarLecturas` en el colector (`req005`); el monitoreo **solo lo muestra**.

## 2. Application (`SolarUY.Application`)

### DTOs (`DTOs/`)
* **`MonitoreoPanelDto`**: `PanelId`, `CodigoPanel`, `Estado` (`EstadoSalud`), `Activo`, `PotenciaActualW` (`double?`), `TemperaturaCelsius` (`double?`), `UltimaLecturaUtc` (`DateTime?`). Los nulos significan "sin lectura reciente".
* **`MonitoreoNodoDto`**: `NodoId`, `Nombre`, `PotenciaActualW`, `Paneles`.
* **`MonitoreoParqueDto`**: `ParqueId`, `Nombre`, `PotenciaActualW`, `Nodos`.
* **`InstantaneaMonitoreoDto`**: `GeneradaUtc`, `Parques`.
* **`AlertaFallaDto`**: `PanelId`, `CodigoPanel`, `NodoNombre`, `ParqueId`.
* Reutiliza **`LecturaTelemetriaDto`** (ya existe) para las lecturas nuevas del detalle del panel.

### `MonitoreoService` (`Services/`)
Depende de `IParqueRepository` e `ITelemetriaRepository`.
* `const int VentanaSegundos = 15`, que equivale a 3 rondas del colector.
* **`ObtenerInstantaneaAsync(ct)`** → `(InstantaneaMonitoreoDto Instantanea, IReadOnlyList<LecturaTelemetria> Lecturas)`:
  1. Trae los parques con nodos y paneles (`ObtenerTodosAsync`, que ya los incluye).
  2. Trae `ObtenerDesdeAsync(ahora - VentanaSegundos)`.
  3. Arma la foto con `ArmarInstantanea`.
* **`ArmarInstantanea(parques, lecturas, ahoraUtc)`** (`static`, **pura**, testeable sin base):
  * Por panel, toma la lectura más reciente de la ventana.
  * La potencia del nodo es la suma de sus paneles **activos con lectura**; la del parque, la suma de sus nodos.
  * El `Estado` sale de la entidad (`PanelSolar.Estado`).
  * Ordena por nombre o código para que la pantalla no "salte".

### `SeguimientoMonitoreo` (`Services/`)
Clase **pura**, sin SignalR. La usa el worker y guarda en memoria **solo** lo último que esta réplica ya mandó.
* **`Comparar(instantanea, lecturas)`** → `CambiosMonitoreo(LecturasNuevas: IReadOnlyList<LecturaTelemetriaDto>, Fallas: IReadOnlyList<AlertaFallaDto>)`:
  * **Lecturas nuevas:** las de `FechaHora` posterior a la última enviada para ese panel.
  * **Fallas:** paneles cuyo estado **pasó** a `FALLA_DETECTADA` respecto de la foto anterior.
  * En la **primera** comparación no hay foto anterior, así que no se generan alertas (evita avisar de todo al arrancar o después de un failover). Criterio 7.
* Que viva en memoria es aceptable: no es dato de usuario y no se pierde nada si cambia la réplica, porque la otra arma su propio seguimiento (regla "sin estado en memoria" de `1_clean_architecture.md`).

## 3. Infrastructure (`SolarUY.Infrastructure`)
* **`TelemetriaRepository.ObtenerDesdeAsync`**: `AsNoTracking().Where(l => l.FechaHora >= desdeUtc).ToListAsync()`.
* Sin migración: el índice `IX_lecturas_telemetria_fecha_hora` ya llega con `req011`.

## 4. Web (`SolarUY.Web`)

### Hub
* **`Hubs/MonitoreoHub : Hub`**, con `[Authorize(Roles = "Administrador,Consulta")]`.
* Se mapea en `app.MapHub<MonitoreoHub>("/hubs/monitoreo")`.
* Métodos que llama el navegador:
  * `SuscribirseMonitoreo()` → `Groups.AddToGroupAsync(ConnectionId, "monitoreo")`.
  * `SuscribirsePanel(Guid panelId)` → grupo `panel-{panelId}`.
* Mensajes del servidor:

| Mensaje | Grupo | Contenido |
|---|---|---|
| `instantanea` | `monitoreo` | `InstantaneaMonitoreoDto` |
| `falla` | `monitoreo` | `AlertaFallaDto` |
| `lectura` | `panel-{id}` | `LecturaTelemetriaDto` |

### Worker
* **`Monitoreo/MonitoreoTiempoRealWorker : BackgroundService`**.
* Opciones `Monitoreo:IntervaloSegundos` (por defecto 5) y `Monitoreo:Habilitado` (por defecto `true`).
* Cada vuelta:
  1. Crea un **scope** (los repositorios son `Scoped`).
  2. Llama `MonitoreoService.ObtenerInstantaneaAsync`, luego `_seguimiento.Comparar`, y envía con `IHubContext<MonitoreoHub>`.
* El `try/catch` de cada vuelta loguea los errores sin matar el worker (mismo patrón que el colector).

### Controlador y vista de monitoreo
* **`MonitoreoController`** (`[Authorize(Roles = "Administrador,Consulta")]`):
  * **`Index(Guid? parqueId)`** renderiza la foto inicial desde el servidor (criterio 9). Con más de un parque, un combo elige cuál ver.
  * **`MonitoreoIndexViewModel`**: `Instantanea`, `Parques` (para el combo) y `ParqueId`.
* **`HomeController.Index`** → `RedirectToAction("Index", "Monitoreo")`. Reemplaza la página de la plantilla (criterio 1).
* **`Views/Monitoreo/Index.cshtml`**:
  * Potencia del parque, una tarjeta por nodo con su potencia y la tabla de sus paneles (código, potencia, temperatura, badge de estado con `EstadoSaludHelper`).
  * Hora de la última actualización e indicador de conexión.
  * `data-panel-id` en cada fila para actualizar en el lugar.
  * **Sin textos explicativos fijos** (criterio 11).
* **`wwwroot/js/monitoreo.js`**:
  * Conexión con `.withAutomaticReconnect()`. En `onreconnected`, vuelve a llamar `SuscribirseMonitoreo`, porque los grupos son por conexión y se pierden al reconectar.
  * `onreconnecting`/`onclose` cambian el indicador a "reconectando" y atenúan los datos (criterio 8).
  * Al recibir `instantanea`, actualiza los valores del parque elegido. Al recibir `falla`, resalta la fila y muestra un *toast* de Bootstrap.
  * Las horas se muestran en `America/Montevideo` con `Intl.DateTimeFormat`.

### Detalle del panel
* **`Views/Paneles/Detalle.cshtml`**: ids o `data-` en el bloque de "Última lectura" y en el `tbody` de las últimas lecturas.
* **`wwwroot/js/panel-detalle.js`**:
  * Se conecta y llama `SuscribirsePanel(id)`, también en `onreconnected`.
  * Al recibir `lectura`, agrega la fila arriba, quita la última y actualiza la última lectura (criterio 6).

### Cliente SignalR
* Archivo `wwwroot/lib/microsoft-signalr/signalr.min.js` (paquete npm `@microsoft/signalr` **8.0.7**, `dist/browser/`), versionado en el repo igual que `bootstrap` y `jquery`.
* El servidor de SignalR viene con ASP.NET Core: **no hace falta un paquete NuGet**.

### Registro en `Program.cs`
* `AddSignalR()`, `AddScoped<MonitoreoService>()`, `AddSingleton<SeguimientoMonitoreo>()`, `AddHostedService<MonitoreoTiempoRealWorker>()` y `MapHub`.

### Menú
* Enlace **Monitoreo** primero en `_Layout.cshtml`, visible para usuarios con sesión.

## 5. nginx (`nginx/nginx.conf`)
Nuevo `location /hubs/`, con las mismas cabeceras `X-Forwarded-*` que `location /` y además:
* `proxy_http_version 1.1;`
* `proxy_set_header Upgrade $http_upgrade;` y `proxy_set_header Connection "upgrade";` (WebSocket).
* `proxy_read_timeout 3600s;` (ya anticipado en el comentario del archivo).
* Si `web1` se cae, el WebSocket se corta, el navegador reconecta y nginx lo manda a `web2`. La cookie de sesión sirve en `web2` porque las claves de Data Protection son compartidas (`req007`).

## 6. Trazabilidad

| Criterio | Dónde se cumple |
|---|---|
| 1 | `MonitoreoController`, `HomeController.Index` (redirect), enlace del menú |
| 2, 3 | `MonitoreoService.ArmarInstantanea` + `Monitoreo/Index.cshtml` |
| 4 | `MonitoreoTiempoRealWorker` + mensaje `instantanea` |
| 5 | `GeneradaUtc` + `monitoreo.js` |
| 6 | `SeguimientoMonitoreo` (lecturas nuevas) + mensaje `lectura` + `panel-detalle.js` |
| 7 | `SeguimientoMonitoreo` (paso a falla, sin alertas en la primera vuelta) + `falla` + toast |
| 8 | `withAutomaticReconnect`, re-suscripción en `onreconnected`, `location /hubs/` en nginx, Data Protection compartida |
| 9 | `MonitoreoController.Index` renderiza la foto inicial |
| 10 | `[Authorize(Roles = "Administrador,Consulta")]` en hub y controlador |
| 11 | Vistas sin textos fijos; la explicación va en la documentación |

## 7. Tests
* **`tests/SolarUY.Application.Tests`** (proyecto nuevo, xUnit, referencia `Application` y `Domain`):
  * `ArmarInstantanea`: sumas por nodo y parque; un panel inactivo o sin lectura no suma; toma la lectura más reciente.
  * `SeguimientoMonitoreo.Comparar`: la primera vuelta no genera alertas; `OPERATIVO → FALLA` genera una alerta; `FALLA → FALLA` no repite; solo devuelve lecturas nuevas.

## 8. Riesgos
* **Hora:** `FechaHora` está en UTC y la ventana se calcula con `DateTime.UtcNow`. Si el reloj del contenedor del colector y el de la web difieren mucho, la ventana puede quedar vacía. Están en la misma máquina, así que es aceptable.
* **Costo:** una consulta cada 5 s por réplica, con 15 paneles, son unas 45 filas. Insignificante.
