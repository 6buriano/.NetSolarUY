# Tareas de Implementación: Recolección de telemetría y diagnóstico

> Documentado después de implementar: las tareas reflejan lo que ya está en `main`.

- [x] **1. Domain**
  - [x] 1.1 `LecturaTelemetria` con validaciones.
  - [x] 1.2 `Nodo.EvaluarLecturas` con los umbrales como constantes.
  - [x] 1.3 `ObtenerConSensoresActivosAsync` y `AgregarVariasAsync` en los repositorios.
  - [x] 1.4 Tests: `NodoEvaluarLecturasTests`, `LecturaTelemetriaTests`.

- [x] **2. Application e Infrastructure**
  - [x] 2.1 `ISensorClient` y `EstadoSensorDto`.
  - [x] 2.2 Cliente HTTP del sensor con timeout de 3 s; devuelve `null` si no responde.
  - [x] 2.3 Índice `(panel_solar_id, fecha_hora)` y migración.

- [x] **3. Collector**
  - [x] 3.1 Proyecto Worker Service y `ColectorOptions`.
  - [x] 3.2 `ColectorTelemetriaWorker`: scope por ronda, emparejado por código, diagnóstico y guardado.
  - [x] 3.3 Tolerancia a fallas: sensor que no responde y ronda que falla.
  - [x] 3.4 Servicio `collector` (una instancia) en `docker-compose.yml`.

- [x] **4. Verificación**
  - [x] 4.1 `dotnet build` y `dotnet test` en verde.
  - [x] 4.2 Prueba con el simulador: las lecturas se guardan cada 5 s y el estado de los paneles cambia al forzar una falla en el simulador. *(confirmar con quien lo implementó)*
  - [x] 4.3 Apagar un sensor del simulador: el colector sigue con los demás. *(confirmar con quien lo implementó)*

- [ ] **5. Pendiente**
  - [ ] 5.1 Responder las preguntas abiertas de `specs.md`.
  - [ ] 5.2 Pasar `verificar-calidad` (ArchUnit + SonarQube) cuando estén integrados.

## Uso de IA
- **Código:** implementado en la iteración 1 con asistencia de IA (Antigravity), sin documentos SDD previos.
- **Este requerimiento, `specs.md` y `tasks.md`:** reconstruidos con asistencia de IA leyendo el código de `main`, y revisados por el equipo. *(Completar: quién lo revisó.)*
