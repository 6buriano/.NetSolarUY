# Especificación Técnica: ABM de Paneles Solares

> Documentado después de implementar, a partir del código de `main`.

## 1. Domain (`SolarUY.Domain`)
* **`PanelSolar`**: `CodigoPanel`, `Modelo`, `Fabricante`, `PotenciaNominalW`, `Activo`, `FechaInstalacion`, `Estado` (`EstadoSalud`), `NodoId`, `Lecturas`, `Mantenimientos`.
  * El constructor valida: código (≤ 20, `[A-Za-z0-9_-]`), modelo y fabricante obligatorios, potencia > 0, estado válido del enum, `NodoId` no vacío. Si algo falla, `ReglaDeNegocioException`.
  * `MarcarActividad(...)` y `CambiarEstado(...)`.
* **`IPanelRepository`**: `ObtenerTodosAsync`, `ObtenerPorNodoAsync`, `ObtenerPorIdAsync`, `ExisteCodigoEnNodoAsync(nodoId, codigo, panelExcluidoId)`, `AgregarAsync`, `ActualizarAsync`, `EliminarAsync`.
* **`ITelemetriaRepository.ObtenerUltimasPorPanelAsync(panelId, cantidad)`**: trae solo las últimas N lecturas (nunca toda la colección, regla de `4_modelado_dominio.md`).

## 2. Application (`SolarUY.Application`)
* **`PanelService`** (depende de `IPanelRepository`, `INodoRepository`, `ITelemetriaRepository`):
  * Listado con filtro por nodo y por estado. ⚠️ El filtro por estado se aplica **en memoria**, después de traer los paneles.
  * `ObtenerLecturasRecientesAsync(panelId, cantidad)`.
  * `CrearAsync` / `ActualizarAsync`: validan que el nodo exista y que el código no esté usado en ese nodo (excluyendo el propio panel al editar).
  * `EliminarAsync`.
* **DTOs**: `PanelDto`, `LecturaTelemetriaDto`, `CrearPanelRequest`, `ActualizarPanelRequest`.

## 3. Infrastructure (`SolarUY.Infrastructure`)
* `PanelRepository` (EF Core).
* Índice **único** compuesto `(nodo_id, codigo_panel)`.
* Cascada panel → lecturas y panel → tareas de mantenimiento.
* Índice `(panel_solar_id, fecha_hora)` en lecturas, para que "las últimas N" sea rápido.

## 4. Web (`SolarUY.Web`)
* **`PanelesController`** (`[Authorize(Roles = "Administrador")]`): `Index(nodoId, estado)`, `Detalle` (con 20 lecturas), `Crear`, `Editar`, `Eliminar` / `EliminarConfirmado`.
* **`PanelFormViewModel`**: DataAnnotations de formato; valores por defecto `PotenciaNominalW = 550` y `Estado = OPERATIVO`.
* Vistas: `Index`, `Detalle`, `Crear`, `Editar`, `Eliminar` y `_Formulario.cshtml`.

## 5. Trazabilidad

| Criterio | Dónde se cumple |
|---|---|
| 1 | `PanelesController.Index` + `PanelService` (filtro de estado en memoria) |
| 2 | `PanelService.ObtenerLecturasRecientesAsync` + `ObtenerUltimasPorPanelAsync` |
| 3, 4, 6, 8 | `PanelFormViewModel` (formato) + constructor de `PanelSolar` |
| 5 | `PanelService` + `ExisteCodigoEnNodoAsync` + índice único `(nodo_id, codigo_panel)` |
| 7 | `PanelService` con `INodoRepository` |
| 9 | `PanelFormViewModel` |
| 10 | Cascadas en `PanelSolarConfiguration` |
| 11 | `TempData["MensajeExito"]` |
| 12 | `[Authorize]` (req002) |

## 6. Tests existentes
* `tests/SolarUY.Domain.Tests/Entities/PanelSolarTests.cs`.

## 7. Preguntas abiertas
* ¿Tiene sentido que el estado se edite a mano, si después el colector lo pisa con cada lectura?
* ¿Conviene pasar el filtro por estado a la consulta del repositorio cuando haya muchos paneles?
