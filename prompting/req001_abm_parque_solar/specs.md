# Especificaciones Técnicas: Gestión de Parques Solares (ABM)

> **Primera versión.** El ABM se implementó antes de adoptar el flujo requirements → specs → tasks, así que este documento se armó a partir del código existente, con asistencia de IA, y lo revisó la responsable del módulo. Sirve para dejar documentado el diseño y detectar qué criterios faltan cubrir. Los próximos requerimientos se especifican antes de programarlos, con la skill `sdd-generar-specs`.

## Preguntas abiertas
- ¿La regla "nombre único" distingue mayúsculas y minúsculas? Propuesta: no ("Parque Norte" y "parque norte" son el mismo parque), que es como compara MySQL por defecto.

## Resumen
ABM de la entidad `ParqueSolar`, del módulo de gestión. Es la raíz de la jerarquía Parque → Nodo → Panel.

## 1. Capa Domain (`SolarUY.Domain`)
- **Entidad `ParqueSolar`** (`Entities/ParqueSolar.cs`)
  - Propiedades con `private set`: `Id` (Guid), `Nombre`, `Ubicacion`, `CapacidadInstaladaKW` (double), `FechaInstalacion` (DateTime), `Nodos` (List&lt;Nodo&gt;).
  - Constructor público `ParqueSolar(nombre, ubicacion, capacidadInstaladaKW, fechaInstalacion)`: genera el `Id` con `Guid.NewGuid()`.
  - Constructor privado sin parámetros, que exige EF Core.
  - `Actualizar(...)`: modifica los datos con las mismas validaciones del alta.
  - `TieneNodos()`: indica si el parque tiene nodos.
  - Invariantes (en `EstablecerDatos`): nombre y ubicación no vacíos, capacidad mayor a 0. Se recortan los espacios y la fecha se guarda sin hora.
- **Excepción `ReglaDeNegocioException`** (`Exceptions/`): se lanza cuando se viola una regla.
- **Interfaz `IParqueRepository`** (`Repositories/`): `ObtenerTodosAsync`, `ObtenerPorIdAsync`, `ExisteAsync`, `ObtenerNombresAsync`, `AgregarAsync`, `ActualizarAsync`, `EliminarAsync`.

## 2. Capa Application (`SolarUY.Application`)
- **DTOs:** `CrearParqueRequest`, `ActualizarParqueRequest` (entrada) y `ParqueDto` (salida, con `CantidadNodos`, `CantidadPaneles` y `PotenciaNominalPanelesKW` calculados).
- **Mapeo:** `ParqueMappings.ToDto()`.
- **Servicio `ParqueService`**, que recibe `IParqueRepository` por constructor:
  - `ObtenerTodosAsync()` y `ObtenerPorIdAsync(id)`.
  - `CrearAsync(request)`: crea la entidad y la guarda. Devuelve el `Id`.
  - `ActualizarAsync(request)`: devuelve `false` si el parque no existe.
  - `EliminarAsync(id)`: lanza `ReglaDeNegocioException` si el parque tiene nodos.

## 3. Capa Infrastructure (`SolarUY.Infrastructure`)
- **`ParqueSolarConfiguration`:** tabla `parques_solares`, columnas en snake_case, `nombre` hasta 150 caracteres, `ubicacion` hasta 200, relación uno a muchos con `nodos` con borrado en cascada (la regla de negocio impide llegar a usarlo).
- **`ParqueRepository`:** implementa `IParqueRepository` con EF Core. Las lecturas del listado usan `AsNoTracking`.
- **Migración:** `MigracionInicial`, compatible (crea la tabla).

## 4. Capa Presentation (`SolarUY.Web`)
- **`ParquesController`**, que usa `ParqueService` y `NodoService` (nunca el `DbContext`):

| Acción | Verbo | Qué hace |
|---|---|---|
| `Index` | GET | Listado |
| `Detalle/{id}` | GET | Datos del parque y sus nodos |
| `Crear` | GET / POST | Formulario de alta |
| `Editar/{id}` | GET / POST | Formulario de edición |
| `Eliminar/{id}` | GET / POST | Confirmación y baja |

- Todas las acciones POST llevan `[ValidateAntiForgeryToken]` y redirigen tras un resultado exitoso, con el mensaje en `TempData`.
- Una `ReglaDeNegocioException` se muestra en el formulario (`ModelState`) o como mensaje de error.
- **`ParqueFormViewModel`:** validaciones de formato (`[Required]`, `[StringLength]`, `[Range]`) y etiquetas en español.
- **Vistas:** `Index`, `Detalle`, `Crear`, `Editar`, `Eliminar` y la parcial `_Formulario`.
- **Autorización:** `[Authorize(Roles = "Administrador")]` sobre la clase (ver `req002_autenticacion_keycloak`).

## Trazabilidad

| Criterio | Dónde se cumple | Estado |
|---|---|---|
| 1–3 Listado | `Index` + `ParqueDto` | ✅ |
| 4 Nombre, ubicación, capacidad obligatorios | `ParqueSolar.EstablecerDatos` + `ParqueFormViewModel` | ✅ |
| 4 **Nombre único** | — | ❌ Pendiente |
| 4 **Fecha no futura** | — | ❌ Pendiente |
| 5 Id generado por el sistema | Constructor de `ParqueSolar` | ✅ |
| 6 Sin nodos al crear | `Nodos` vacío al construir | ✅ |
| 7 Errores en el formulario | `ParquesController.Crear` | ✅ |
| 8 Detalle | `Detalle` + `NodoService.ObtenerPorParqueAsync` | ✅ |
| 9–10 Modificación | `ParqueSolar.Actualizar` + `Editar` | ✅ |
| 11 Confirmación | Vista `Eliminar` | ✅ |
| 12 No borrar con nodos | `ParqueService.EliminarAsync` | ✅ |
| 13 Rol Administrador | `[Authorize(Roles = "Administrador")]` en `ParquesController` (req002) | ✅ |

## Diseño propuesto para lo pendiente

**Nombre único**
- Domain: agregar a `IParqueRepository` el método `ExisteConNombreAsync(string nombre, Guid? excluirId)`.
- Infrastructure: implementarlo en `ParqueRepository`; en `ParqueSolarConfiguration`, cambiar el índice de `Nombre` a `.IsUnique()` y generar una migración. Es compatible con la versión anterior siempre que no existan nombres repetidos en la base.
- Application: en `ParqueService.CrearAsync` y `ActualizarAsync`, si el nombre ya existe, lanzar `ReglaDeNegocioException("Ya existe un parque con ese nombre.")`.

**Fecha no futura**
- Domain: en `ParqueSolar.EstablecerDatos`, lanzar `ReglaDeNegocioException` si `fechaInstalacion.Date > DateTime.Today`, con su prueba unitaria.
