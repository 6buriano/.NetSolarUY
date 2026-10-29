# Tareas de Implementación: Gestión de tareas de mantenimiento

> Documentado después de implementar: las tareas reflejan lo que ya está en `main` (commit `f73e917`).

- [x] **1. Domain**
  - [x] 1.1 `TipoMantenimiento` (`LIMPIEZA`, `REPARACION`, `INSPECCION_PREVENTIVA`, `REEMPLAZO`) y `EstadoMantenimiento` (`PENDIENTE`, `EN_PROCESO`, `COMPLETADO`, `CANCELADO`).
  - [x] 1.2 `TareaMantenimiento` con `set` privados, constructor privado para EF Core y constructor público con validaciones.
  - [x] 1.3 `TareaMantenimiento.Reprogramar`: rechaza `COMPLETADO`, exige descripción.
  - [x] 1.4 `TareaMantenimiento.Iniciar`: solo desde `PENDIENTE`.
  - [x] 1.5 `TareaMantenimiento.Completar(fechaRealizada)`: solo desde `EN_PROCESO`, guarda la fecha.
  - [x] 1.6 `TareaMantenimiento.Cancelar`: rechaza `COMPLETADO`.
  - [x] 1.7 `IMantenimientoRepository` (`ObtenerTodosAsync`, `ObtenerPorPanelAsync`, `ObtenerPorIdAsync`, `AgregarAsync`, `ActualizarAsync`).
  - [x] 1.8 `IPanelRepository.ObtenerDescripcionesAsync` para resolver el nombre del panel.
  - [x] 1.9 Tests de dominio: `tests/SolarUY.Domain.Tests/Entities/TareaMantenimientoTests.cs` con un caso por regla de transición y por validación del constructor.

- [x] **2. Infrastructure**
  - [x] 2.1 `TareaMantenimientoConfiguration`: tabla `mantenimientos`, columnas `snake_case`, enums como `varchar(30)`, `descripcion` 500 y `personal_asignado` 150.
  - [x] 2.2 `DbSet<TareaMantenimiento> TareasMantenimiento` en `SolarUYDbContext`.
  - [x] 2.3 Cascada panel → tareas en `PanelSolarConfiguration` e índice `IX_mantenimientos_panel_solar_id`.
  - [x] 2.4 `MantenimientoRepository`: lecturas con `AsNoTracking`, orden por fecha, `ObtenerPorIdAsync` trackeado y `ActualizarAsync` solo con `SaveChangesAsync`.
  - [x] 2.5 Registro `services.AddScoped<IMantenimientoRepository, MantenimientoRepository>()` en `DependencyInjection`.
  - [x] 2.6 Migración: la tabla se crea en `20261012172223_MigracionInicial` (sin migración propia de `req009`).

- [x] **3. Application**
  - [x] 3.1 `CrearMantenimientoRequest`.
  - [x] 3.2 `ReprogramarMantenimientoRequest` (sin panel ni tipo).
  - [x] 3.3 `MantenimientoDto`.
  - [x] 3.4 `MantenimientoMappings.ToDto(tarea, panelDescripcion)`.
  - [x] 3.5 `MantenimientoService.ObtenerTodosAsync(estado)`.
  - [x] 3.6 `MantenimientoService.ObtenerPorPanelAsync(panelSolarId)`.
  - [x] 3.7 `MantenimientoService.ObtenerPorIdAsync(id)`.
  - [x] 3.8 `MantenimientoService.CrearAsync`: valida que el panel exista y devuelve el `Id`.
  - [x] 3.9 `MantenimientoService.ReprogramarAsync`: `false` si no existe.
  - [x] 3.10 `MantenimientoService.IniciarAsync` / `CompletarAsync` (`DateTime.UtcNow`) / `CancelarAsync`, con el `CambiarEstadoAsync` privado.

- [x] **4. Registro de dependencias**
  - [x] 4.1 `builder.Services.AddScoped<MantenimientoService>()` en `src/SolarUY.Web/Program.cs`.

- [x] **5. Presentation (MVC)**
  - [x] 5.1 `MantenimientoFormViewModel` con DataAnnotations y valores por defecto (`INSPECCION_PREVENTIVA`, hoy).
  - [x] 5.2 `MantenimientosIndexViewModel`.
  - [x] 5.3 `EstadoMantenimientoHelper.ClaseBadge`.
  - [x] 5.4 `MantenimientosController` con `[Authorize(Roles = "Administrador")]` e `Index(panelId, estado)`.
  - [x] 5.5 `MantenimientosController.Detalle`.
  - [x] 5.6 `MantenimientosController.Crear` (GET y POST, con antiforgery y `CargarPanelesAsync`).
  - [x] 5.7 `MantenimientosController.Editar` (GET y POST): bloquea `COMPLETADO`, valida que `id` coincida.
  - [x] 5.8 `MantenimientosController.Iniciar` / `Completar` / `Cancelar` con el helper `CambiarEstadoAsync`.
  - [x] 5.9 Vista `Views/Mantenimientos/Index.cshtml` con los dos filtros y la tabla.
  - [x] 5.10 Vista `Views/Mantenimientos/Detalle.cshtml` con los botones por estado y "Reprogramar" solo si corresponde.
  - [x] 5.11 Vistas `Crear.cshtml`, `Editar.cshtml` y el parcial `_Formulario.cshtml` (panel y tipo deshabilitados al reprogramar, con `input hidden`).
  - [x] 5.12 Historial en el detalle del panel: `PanelDetalleViewModel.Mantenimientos`, `PanelesController.Detalle` y la tabla de `Views/Paneles/Detalle.cshtml`.
  - [x] 5.13 Enlace "Mantenimiento" en `Views/Shared/_Layout.cshtml`.

- [x] **6. Verificación**
  - [x] 6.1 `dotnet build` en verde.
  - [x] 6.2 `dotnet test` en verde (sin tests nuevos: no hay ninguno de mantenimiento).
  - [x] 6.3 Prueba manual del alta, la reprogramación, el ciclo `PENDIENTE` → `EN_PROCESO` → `COMPLETADO`, la cancelación y los filtros. *(confirmar con quien lo implementó)*
  - [x] 6.4 Prueba manual del historial en el detalle del panel. *(confirmar con quien lo implementó)*

- [ ] **7. Pendiente**
  - [x] 7.1 Agregar `TareaMantenimientoTests.cs` (tarea 1.9) y volver a correr `dotnet test` (109 pruebas del dominio en verde).
  - [ ] 7.2 Responder las preguntas abiertas de `specs.md`.
  - [ ] 7.3 Pasar `verificar-calidad` (ArchUnit + SonarQube) cuando estén integrados.

## Uso de IA
- **Código:** implementado en la iteración 1 con asistencia de IA, sin documentos SDD previos; commit `f73e917` ("Gestion de tareas de mantenimiento y vincular mant a panel/nodo e historial").
- **Este requerimiento, `specs.md` y `tasks.md`:** reconstruidos con asistencia de IA leyendo el código de `main`, y revisados por el equipo. *(Completar: quién lo revisó.)*
- **Sin confirmar leyendo el código:** si existió un `specs.md` previo de este requerimiento. Un comentario de `TareaMantenimiento.PersonalAsignado` dice "Campo que agrega el diagrama de clases por sobre specs.md", así que se escribió contra algún diseño, pero no quedó en `prompting/`.
