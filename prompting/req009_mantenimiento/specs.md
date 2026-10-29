# Especificación Técnica: Gestión de tareas de mantenimiento

> Documentado después de implementar, a partir del código de `main` (commit `f73e917`).

## Preguntas abiertas
Ninguna bloquea lo ya implementado; quedan para la próxima iteración.
* ¿La fecha programada tiene que ser futura? Hoy no se valida, se acepta cualquier fecha.
* ¿Se puede volver de `CANCELADO` a `PENDIENTE`? Hoy no: cancelar es definitivo (y cancelar una tarea ya cancelada no falla, deja el mismo estado).
* Al eliminar un panel se borran sus tareas en cascada, incluidas las `COMPLETADO`: se pierde el historial. ¿Conviene conservarlo?

## Resumen
Módulo de **mantenimiento** sobre el agregado `PanelSolar`. Una entidad `TareaMantenimiento` con su ciclo de vida (reglas en el dominio), un repositorio propio, un servicio de casos de uso que la resuelve con el nombre del panel, y el ABM en MVC con acciones de iniciar, completar, cancelar y reprogramar. El detalle del panel suma su historial.

## 1. Capa Domain (`SolarUY.Domain`)
* **`TareaMantenimiento`** (entidad, `Id` `Guid`, todos los `set` privados):
  | Propiedad | Tipo | Nota |
  |---|---|---|
  | `Id` | `Guid` | `Guid.NewGuid()` en el constructor |
  | `PanelSolarId` | `Guid` | obligatorio, no vacío |
  | `Tipo` | `TipoMantenimiento` | enum validado |
  | `FechaProgramada` | `DateTime` | obligatoria |
  | `FechaRealizada` | `DateTime?` | `null` hasta completar |
  | `Descripcion` | `string` | obligatoria, se guarda con `Trim()` |
  | `Estado` | `EstadoMantenimiento` | nace `PENDIENTE` |
  | `PersonalAsignado` | `string?` | opcional, `Trim()` o `null` si viene en blanco |
  * Constructor privado sin parámetros para que EF Core materialice.
  * Constructor público: valida panel no vacío, tipo definido en el enum y descripción no vacía. Si algo falla, `ReglaDeNegocioException`.
  * **`Reprogramar(fechaProgramada, descripcion, personalAsignado)`**: rechaza si `Estado == COMPLETADO`; exige descripción; actualiza fecha, descripción y personal. No toca `PanelSolarId` ni `Tipo`.
  * **`Iniciar()`**: exige `Estado == PENDIENTE` y pasa a `EN_PROCESO`.
  * **`Completar(fechaRealizada)`**: exige `Estado == EN_PROCESO`, pasa a `COMPLETADO` y guarda `FechaRealizada`.
  * **`Cancelar()`**: rechaza si `Estado == COMPLETADO`; pasa a `CANCELADO`.
  * Toda violación lanza `ReglaDeNegocioException` con el mensaje que ve el usuario.
* **`TipoMantenimiento`**: `LIMPIEZA`, `REPARACION`, `INSPECCION_PREVENTIVA`, `REEMPLAZO`.
* **`EstadoMantenimiento`**: `PENDIENTE`, `EN_PROCESO`, `COMPLETADO`, `CANCELADO`.
* **`PanelSolar.Mantenimientos`**: colección de navegación, para la cascada y el historial.
* **`IMantenimientoRepository`**:
  * `ObtenerTodosAsync(EstadoMantenimiento? estado, ct)`.
  * `ObtenerPorPanelAsync(Guid panelSolarId, ct)`.
  * `ObtenerPorIdAsync(Guid id, ct)` → `TareaMantenimiento?`.
  * `AgregarAsync(TareaMantenimiento, ct)`.
  * `ActualizarAsync(TareaMantenimiento, ct)`: la entidad ya viene trackeada de `ObtenerPorIdAsync`; el repositorio solo persiste.
* **`IPanelRepository.ObtenerDescripcionesAsync(ct)`** → `IReadOnlyDictionary<Guid, string>` con `"P01 (JAM72S30-550/MR)"`. Se reutiliza el de `req004` para no cargar el panel entero solo para mostrar su nombre.

## 2. Capa Application (`SolarUY.Application`)
* **DTOs**:
  * `CrearMantenimientoRequest(PanelSolarId, Tipo, FechaProgramada, Descripcion, PersonalAsignado)`.
  * `ReprogramarMantenimientoRequest(Id, FechaProgramada, Descripcion, PersonalAsignado)`: **no lleva** panel ni tipo, porque no se cambian al reprogramar.
  * `MantenimientoDto(Id, PanelSolarId, PanelDescripcion?, Tipo, FechaProgramada, FechaRealizada?, Descripcion, Estado, PersonalAsignado?)`.
* **`MantenimientoMappings.ToDto(this TareaMantenimiento, string? panelDescripcion)`**: el nombre del panel se pasa como parámetro porque se resuelve fuera del agregado.
* **`MantenimientoService`** (depende de `IMantenimientoRepository` y `IPanelRepository`):
  * `ObtenerTodosAsync(estado, ct)`: filtra por estado, trae el diccionario de descripciones y mapea a DTO.
  * `ObtenerPorPanelAsync(panelSolarId, ct)`: igual, para el historial del panel.
  * `ObtenerPorIdAsync(id, ct)` → `MantenimientoDto?`.
  * `CrearAsync(request, ct)` → `Guid`: valida que el panel exista (`ReglaDeNegocioException` si no), construye la entidad y la guarda. Devuelve el `Id` para redirigir al detalle.
  * `ReprogramarAsync(request, ct)` → `bool`: `false` si la tarea no existe; si existe delega en `Reprogramar` del dominio y persiste.
  * `IniciarAsync` / `CompletarAsync` / `CancelarAsync` → `bool`: pasan por un `CambiarEstadoAsync` privado que busca la tarea, aplica la acción del dominio y persiste. `CompletarAsync` pasa `DateTime.UtcNow`.
  * El servicio **no repite** las reglas de transición: las deja fallar en el dominio.

## 3. Capa Infrastructure (`SolarUY.Infrastructure`)
* **`TareaMantenimientoConfiguration`**: tabla `mantenimientos`. Columnas `snake_case`: `id`, `panel_solar_id`, `tipo`, `fecha_programada`, `fecha_realizada`, `descripcion`, `estado`, `personal_asignado`.
  * `tipo` y `estado` con `HasConversion<string>()` y `varchar(30)`, para que la tabla se lea sin traducir enums.
  * `descripcion` `varchar(500)` obligatoria; `personal_asignado` `varchar(150)` opcional.
* **`PanelSolarConfiguration`**: `HasMany(Mantenimientos).WithOne().HasForeignKey(PanelSolarId).OnDelete(Cascade)`. Índice `IX_mantenimientos_panel_solar_id` sobre la FK, que es el que usan el filtro por panel y el historial.
* **`MantenimientoRepository`**:
  * Lecturas con `AsNoTracking()`.
  * `ObtenerTodosAsync`: ordena por `FechaProgramada` ascendente y filtra por estado en SQL cuando se lo pasan.
  * `ObtenerPorPanelAsync`: ordena por `FechaProgramada` descendente.
  * `ObtenerPorIdAsync` **sin** `AsNoTracking`, porque el contexto tiene que trackear la entidad para `ActualizarAsync`.
  * `ActualizarAsync` solo hace `SaveChangesAsync`.
* **`SolarUYDbContext`**: `DbSet<TareaMantenimiento> TareasMantenimiento`.
* **Migración**: la tabla `mantenimientos` se creó en `20261012172223_MigracionInicial`. **No hay migración propia de `req009`** (la funcionalidad es de la iteración 1). Es aditiva: crear la tabla y su índice no rompe datos existentes, así que una base anterior se actualiza sin pasos manuales.

## 4. Capa Presentation (MVC, `SolarUY.Web`)
* **`MantenimientosController`** con `[Authorize(Roles = "Administrador")]`; inyecta `MantenimientoService` y `PanelService`.
  | Acción | Verbo / ruta | Comportamiento |
  |---|---|---|
  | `Index(Guid? panelId, EstadoMantenimiento? estado)` | GET `/Mantenimientos` | Filtra por estado en el servicio y por panel **en memoria**; llena el ViewModel con paneles y filtros |
  | `Detalle(Guid id)` | GET `/Mantenimientos/Detalle/{id}` | `NotFound()` si no existe |
  | `Crear(Guid? panelId)` | GET `/Mantenimientos/Crear` | Precarga el panel si viene por querystring |
  | `Crear(MantenimientoFormViewModel)` | POST, antiforgery | Si el modelo no valida recarga los paneles; `ReglaDeNegocioException` → `ModelState`; éxito → `TempData["MensajeExito"]` y redirect al detalle |
  | `Editar(Guid id)` | GET `/Mantenimientos/Editar/{id}` | `NotFound()` si no existe; si está `COMPLETADO` → `TempData["MensajeError"]` y vuelve al detalle |
  | `Editar(Guid id, MantenimientoFormViewModel)` | POST, antiforgery | `NotFound()` si `id` no coincide con el del formulario o si la tarea desapareció |
  | `Iniciar(Guid id)` / `Completar(Guid id)` / `Cancelar(Guid id)` | POST, antiforgery | Comparten `CambiarEstadoAsync`; `TempData["MensajeError"]` con el mensaje del dominio y redirect al detalle |
  * `CargarPanelesAsync` llena el desplegable en las dos vistas de formulario.
* **`MantenimientoFormViewModel`**: `Id?`, `PanelSolarId` (`[Required]`, por defecto `Guid.Empty`), `Tipo` (por defecto `INSPECCION_PREVENTIVA`), `FechaProgramada` (`[Required]`, `DataType.Date`, por defecto hoy), `Descripcion` (`[Required]`, 500), `PersonalAsignado` (150, opcional) y `Paneles` (no se postea). `Id` con valor significa "reprogramar".
* **`MantenimientosIndexViewModel`**: `Tareas`, `Paneles`, `PanelSolarId`, `Estado`.
* **`EstadoMantenimientoHelper.ClaseBadge(estado)`**: `PENDIENTE` → gris, `EN_PROCESO` → amarillo, `COMPLETADO` → verde, `CANCELADO` → rojo.
* **Vistas** en `Views/Mantenimientos/`:
  * `Index`: dos desplegables que se autoenvían (`panelId`, `estado`), enlace "Limpiar", tabla ordenada por fecha programada descendente y mensaje cuando no hay resultados.
  * `Detalle`: datos en una lista de definición y botones según el estado (iniciar/cancelar, completar/cancelar); "Reprogramar" solo si no está `COMPLETADO` ni `CANCELADO`.
  * `Crear` y `Editar`: formulario en una tarjeta con `_Formulario` y validación del lado del cliente.
  * `_Formulario`: panel y tipo van `disabled` al reprogramar y se reenvían con un `input hidden`, porque un `select` deshabilitado no se postea.
* **Historial en el panel**: `PanelesController.Detalle` pide `ObtenerPorPanelAsync` y lo deja en `PanelDetalleViewModel.Mantenimientos`; `Views/Paneles/Detalle.cshtml` lo muestra en una tabla con el badge de estado, el enlace "Ver" a cada tarea y el botón "Nueva tarea" con `panelId`.
* **Menú**: entrada "Mantenimiento" en `Views/Shared/_Layout.cshtml`.
* **Registro**: `builder.Services.AddScoped<MantenimientoService>()` en `Program.cs`; el repositorio, en `AddInfrastructure`.

## 5. Trazabilidad

| Criterio | Dónde se cumple |
|---|---|
| 1 | `MantenimientoRepository.ObtenerTodosAsync` (estado) + `MantenimientosController.Index` (panel) |
| 2 | `Views/Mantenimientos/Index.cshtml` |
| 3 | `MantenimientoFormViewModel` (`[Required]`) + constructor de `TareaMantenimiento` |
| 4 | `TipoMantenimiento` + `MantenimientoFormViewModel.Tipo` (valor por defecto) |
| 5 | `[StringLength(500)]` + `Descripcion` obligatoria en el dominio |
| 6 | `[StringLength(150)]` + `personal_asignado varchar(150)` |
| 7 | `MantenimientoService.CrearAsync` (`ObtenerPorIdAsync` del panel) |
| 8 | Constructor de `TareaMantenimiento` (`Estado = PENDIENTE`) |
| 9 | `Iniciar`, `Completar`, `Cancelar` + acciones del controlador |
| 10 | `TareaMantenimiento.Iniciar` |
| 11 | `TareaMantenimiento.Completar` |
| 12 | `TareaMantenimiento.Reprogramar` + guarda en `MantenimientosController.Editar` |
| 13 | `TareaMantenimiento.Cancelar` |
| 14 | `ReprogramarMantenimientoRequest` (sin panel ni tipo) + `_Formulario.cshtml` (`disabled`) |
| 15 | `CompletarAsync` (`DateTime.UtcNow`) → `FechaRealizada` → `Detalle.cshtml` |
| 16 | Botones por estado en `Views/Mantenimientos/Detalle.cshtml` + `EstadoMantenimientoHelper` |
| 17 | Condición del botón "Reprogramar" en `Detalle.cshtml` |
| 18 | `PanelesController.Detalle` + `ObtenerPorPanelAsync` + tabla de `Views/Paneles/Detalle.cshtml` |
| 19 | `TempData["MensajeExito"]` / `TempData["MensajeError"]` en el controlador |
| 20 | Cascada en `PanelSolarConfiguration` (req004) |
| 21 | `[Authorize(Roles = "Administrador")]` (req002) |

## 6. Tests existentes
* `tests/SolarUY.Domain.Tests/Entities/TareaMantenimientoTests.cs` (agregado después de la primera versión de este documento): cubre las validaciones del constructor, las cinco reglas de transición y la reprogramación, incluido el caso de cancelar dos veces.
