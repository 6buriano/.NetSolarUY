# Especificaciones Técnicas: Cierre de las tareas de mantenimiento

> Escrito **antes** de implementar, a partir de la devolución del docente y del `req009` (mantenimiento).

## 1. Alcance

Se agrega al cierre de una tarea lo que hoy no existe: la **observación de cierre** (qué se hizo) y la
**fecha real de realización** elegida por el usuario. No cambia el ciclo de vida del req009 ni el resto de las
pantallas.

## 2. Domain

**`TareaMantenimiento`**

| Miembro | Cambio |
|---|---|
| `ObservacionesCierre : string?` | Nuevo. Nulo hasta que la tarea se completa. |
| `LargoMaximoObservaciones` | Nuevo, `const int = 500`, compartido con la configuración de EF y con la vista. |
| `Completar(DateTime fechaRealizada, string? observaciones, DateTime ahoraUtc)` | Antes era `Completar(DateTime)`. Valida, en este orden: estado `EN_PROCESO`; fecha distinta de `default`; fecha no posterior a `ahoraUtc`; observación de hasta `LargoMaximoObservaciones`. Recorta la observación y la deja nula si queda vacía. |

**Por qué la hora actual entra como parámetro.** El dominio no toma el reloj por su cuenta: quien llama le pasa
`ahoraUtc` (el servicio lo obtiene de `TimeProvider`). Así la regla «no puede ser futura» vive en el dominio y
se puede probar con una fecha fija.

## 3. Infrastructure

- `TareaMantenimientoConfiguration`: `ObservacionesCierre` → columna `observaciones_cierre`, `varchar(500)`,
  nullable.
- Migración `CierreDeMantenimiento`: **sólo** agrega la columna (`AddColumn`), sin tocar índices ni datos
  existentes. Se aplica con el perfil `migracion`, como el resto.

## 4. Application

- `MantenimientoDto`: nuevo campo `ObservacionesCierre` al final (los que ya existían no cambian de posición).
- `MantenimientoMappings.ToDto`: lo copia.
- `MantenimientoService`: nuevo constructor con `TimeProvider`; `CompletarAsync(Guid id, DateTime fechaRealizada,
  string? observaciones, CancellationToken)` llama a `tarea.Completar(fechaRealizada, observaciones,
  _reloj.GetUtcNow().UtcDateTime)`.

## 5. Presentation

- `Models/CierreMantenimientoViewModel`: `FechaRealizada` (obligatoria) y `ObservacionesCierre`
  (`StringLength(LargoMaximoObservaciones)`).
- `MantenimientosController.Completar` pasa a recibir ese modelo, valida `ModelState`, convierte la hora del
  formulario (Montevideo) a UTC con `HoraLocal.AParaUtc` y llama al servicio. Los errores de regla de negocio
  vuelven como `TempData["MensajeError"]`, igual que el resto de las acciones de estado.
- `Views/Mantenimientos/Detalle.cshtml`: cuando la tarea está `EN_PROCESO`, el botón **Completar** se reemplaza
  por un formulario con la fecha propuesta (ahora, en hora de Montevideo) y el campo de observaciones. El
  detalle muestra las observaciones del cierre.
- `HoraLocal.AParaUtc`: camino inverso de `DesdeUtc`, para no restar horas a mano en el controlador.

## 6. Tests

- `TareaMantenimientoTests`: completar guarda fecha y observaciones (recortadas), observaciones vacías quedan
  nulas, observaciones de más de 500 caracteres fallan, fecha futura falla, fecha vacía falla, y las
  transiciones inválidas siguen fallando.

## 7. Riesgos

- **Migración antes de la entrega**: es una columna nullable, así que no rompe filas existentes ni requiere
  migrar datos. El despliegue la aplica con el perfil `migracion`.
- **Zona horaria**: el usuario escribe hora de Montevideo y el sistema guarda UTC. La conversión está en
  `HoraLocal`, igual que la de lectura; si el servidor no tiene la base de zonas, cae a UTC-3 fijo.
- **No se puede volver atrás** una vez completada: es la misma regla del req009 y se mantiene a propósito.
