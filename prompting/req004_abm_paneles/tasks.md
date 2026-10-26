# Tareas de Implementación: ABM de Paneles Solares

> Documentado después de implementar: las tareas reflejan lo que ya está en `main`.

- [x] **1. Domain**
  - [x] 1.1 Entidad `PanelSolar` con `private set` y validaciones en el constructor.
  - [x] 1.2 Métodos `MarcarActividad` y `CambiarEstado`.
  - [x] 1.3 `IPanelRepository` con `ExisteCodigoEnNodoAsync`.
  - [x] 1.4 `ITelemetriaRepository.ObtenerUltimasPorPanelAsync`.
  - [x] 1.5 Tests: `PanelSolarTests`.

- [x] **2. Application**
  - [x] 2.1 DTOs y requests (`PanelDto`, `LecturaTelemetriaDto`, `CrearPanelRequest`, `ActualizarPanelRequest`).
  - [x] 2.2 `PanelService` (listado con filtros, lecturas recientes, alta y edición con validaciones, baja).

- [x] **3. Infrastructure**
  - [x] 3.1 Configuración EF: índice único `(nodo_id, codigo_panel)`, cascadas a lecturas y mantenimiento.
  - [x] 3.2 `PanelRepository` y registro en `AddInfrastructure`.
  - [x] 3.3 Migración.

- [x] **4. Web**
  - [x] 4.1 `PanelFormViewModel` con valores por defecto.
  - [x] 4.2 `PanelesController`.
  - [x] 4.3 Vistas y `_Formulario.cshtml`; el detalle muestra las últimas 20 lecturas.

- [x] **5. Verificación**
  - [x] 5.1 `dotnet build` y `dotnet test` en verde.
  - [x] 5.2 Prueba manual: alta, código repetido en el mismo nodo (rechazado) y en otro nodo (aceptado), filtros, baja. *(confirmar con quien lo implementó)*

- [ ] **6. Pendiente**
  - [ ] 6.1 Responder las preguntas abiertas de `specs.md`.
  - [ ] 6.2 Pasar `verificar-calidad` (ArchUnit + SonarQube) cuando estén integrados.

## Uso de IA
- **Código:** implementado en la iteración 1 con asistencia de IA (Antigravity), sin documentos SDD previos.
- **Este requerimiento, `specs.md` y `tasks.md`:** reconstruidos con asistencia de IA leyendo el código de `main`, y revisados por el equipo. *(Completar: quién lo revisó.)*
