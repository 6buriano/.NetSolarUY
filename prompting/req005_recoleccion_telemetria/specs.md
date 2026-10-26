# Especificación Técnica: Recolección de telemetría y diagnóstico

> Documentado después de implementar, a partir del código de `main`.

## 1. Domain (`SolarUY.Domain`)
* **`Nodo.EvaluarLecturas(lecturas)`**: aplica la regla del criterio 4 a todas las lecturas de una ronda del nodo y deja el `EstadoDetectado` en cada `LecturaTelemetria` y el `Estado` en cada `PanelSolar`.
  * Constantes con nombre: `TemperaturaMaximaCelsius = 75`, `UmbralRendimientoBajo = 0.15`, `UmbralFallaDetectada = 0.40`, `RendimientoMinimoParaEvaluarCaida = 0.05`, `ToleranciaDeComparacion = 1e-9` (para no caer de umbral por un error de redondeo).
  * La comparación es **contra los otros paneles** (sin incluir al panel evaluado) y normalizada por potencia nominal, así paneles de distinta potencia se pueden comparar.
* **`LecturaTelemetria`**: valida sus valores en el constructor.
* **`INodoRepository.ObtenerConSensoresActivosAsync()`**, **`ITelemetriaRepository.AgregarVariasAsync(lecturas)`**.

## 2. Application (`SolarUY.Application`)
* **`ISensorClient.ObtenerEstadoAsync(urlBase, identificadorHardwareARM)`** → `EstadoSensorDto` (paneles con sus mediciones y `VolcadoRedActivo`), o `null` si el sensor no respondió.

## 3. Infrastructure (`SolarUY.Infrastructure`)
* Cliente HTTP de `ISensorClient` con timeout de 3 s (`DependencyInjection.TimeoutSensor`), para que un sensor caído no frene la ronda.
* Índice `(panel_solar_id, fecha_hora)` en `lecturas_telemetria`.

## 4. Collector (`SolarUY.Collector`)
* **`ColectorTelemetriaWorker : BackgroundService`** con `ColectorOptions` (`Habilitado`, `IntervaloSegundos`, mínimo 1).
* Cada ronda:
  1. Crea **un scope de DI** (el `DbContext` y los repositorios son `Scoped`; el worker es `Singleton`).
  2. `ObtenerConSensoresActivosAsync`.
  3. Por cada nodo: `ObtenerEstadoAsync` → si es `null`, log de advertencia y `continue`.
  4. `ArmarLecturas`: empareja por `BuscarPanelPorCodigo`, sincroniza `MarcarActividad` y crea las `LecturaTelemetria`.
  5. `nodo.EvaluarLecturas(lecturas)` y `nodo.Inversor.EstablecerVolcadoRed(...)`.
  6. Guarda primero los paneles (`ActualizarAsync`) y después las lecturas (`AgregarVariasAsync`).
* Un `try/catch` por ronda: cualquier excepción se loguea y el bucle sigue.
* `Program.cs` del Collector registra `AddInfrastructure` (única referencia permitida a `Infrastructure`, regla de `1_clean_architecture.md`).

## 5. Trazabilidad

| Criterio | Dónde se cumple |
|---|---|
| 1 | `ColectorOptions` + bucle de `ExecuteAsync` + `ObtenerConSensoresActivosAsync` |
| 2 | `ArmarLecturas` + `Nodo.BuscarPanelPorCodigo` |
| 3 | `LecturaTelemetria` + `AgregarVariasAsync` |
| 4 | `Nodo.EvaluarLecturas` |
| 5 | `PanelSolar.CambiarEstado` (desde `EvaluarLecturas`) + `Inversor.EstablecerVolcadoRed` |
| 6 | `ObtenerEstadoAsync` devuelve `null` → `continue` |
| 7 | `try/catch` de la ronda en `ExecuteAsync` |
| 8 | `docker-compose.yml` (un solo servicio `collector`) |

## 6. Tests existentes
* `tests/SolarUY.Domain.Tests/Entities/NodoEvaluarLecturasTests.cs` (casos de cada umbral, temperatura, panel inactivo y "de noche").
* `LecturaTelemetriaTests.cs`.

## 7. Preguntas abiertas
* La tabla de lecturas crece sin límite (~260.000 filas por día con 15 paneles). ¿Política de retención o agregación para el histórico?
* En la próxima iteración las lecturas se van a mandar en vivo por SignalR: definir si lo publica el colector o la web.
