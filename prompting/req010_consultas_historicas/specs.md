# Especificación Técnica: Consultas históricas de generación

> Documentado después de implementar, a partir del código de `main`.

## Preguntas abiertas
* La estimación de energía **no avisa cuando el colector estuvo caído**: un período con pocas lecturas se ve igual que uno con generación baja. ¿Conviene marcarlo (por ejemplo, comparando las lecturas del período con las esperadas a 5 s)?
* No se valida que «desde» sea anterior a «hasta»: la consulta devuelve la tabla vacía y se muestra el aviso genérico de «no hay lecturas». ¿Hace falta un mensaje propio?
* `ReporteService` y `ObtenerAgregadoPorPanelAsync` **no tienen tests**: la energía y las etiquetas se pueden probar sin base.

## Resumen
Pantalla de solo lectura que agrega las lecturas de telemetría de un panel por día, mes o año y estima la energía de cada período. Vive en el módulo de telemetría y reutiliza lo que ya guarda el colector (`req005`): no agrega entidades, tablas ni migraciones.

## 1. Capa Domain (`SolarUY.Domain`)
* **`GranularidadConsulta`** (`Enums/`, enum): `DIA`, `MES`, `ANIO`. Vive en el dominio porque define cómo se agrupa la telemetría, no cómo se dibuja.
* **`AgregadoPotencia`** (`Repositories/`, `record` `sealed`): es el resultado crudo de la consulta, no una entidad y no se persiste. Campos: `PeriodoInicio` (`DateTime`), `CantidadLecturas` (`int`), `PotenciaPromedioW`, `PotenciaMaximaW` y `PotenciaSumaW` (`double`, en W). Está junto al contrato porque es lo que el contrato devuelve.
* **`ITelemetriaRepository.ObtenerAgregadoPorPanelAsync(panelSolarId, granularidad, desde?, hasta?, ct)`** → `IReadOnlyList<AgregadoPotencia>`.
  * `desde` y `hasta` son `DateTime?`: `null` significa «sin límite» de ese lado.
  * Devuelve un elemento por período **con datos**, del período más reciente al más antiguo.
  * No arma etiquetas ni calcula energía: eso es de Application. El dominio no sabe cómo se muestra.
* Sin cambios en las entidades existentes ni en sus invariantes.

## 2. Capa Application (`SolarUY.Application`)
* **`ResumenGeneracionDto`** (`DTOs/`, `record`): `PeriodoInicio` (`DateTime`), `Etiqueta` (`string`), `CantidadLecturas` (`int`), `PotenciaPromedioW`, `PotenciaMaximaW` y `EnergiaEstimadaKWh` (`double`). Es la fila de la tabla, ya lista para mostrar.
* **`ReporteService`** (depende solo de `ITelemetriaRepository`):
  * **`const int IntervaloLecturaSegundos = 5`**: cada cuántos segundos guarda una lectura el colector. Es el **único supuesto** de la estimación y está documentado en el propio código; la vista lo muestra leyendo esta constante.
  * **`ObtenerGeneracionPorPanelAsync(panelSolarId, granularidad, desde?, hasta?, ct)`** → `IReadOnlyList<ResumenGeneracionDto>`. Pasos:
    1. Pide los agregados al repositorio, con el mismo rango.
    2. Por cada agregado arma la etiqueta con `Etiquetar`.
    3. Calcula la energía: `PotenciaSumaW × IntervaloLecturaSegundos / 3600` da Wh; se divide por 1000 para kWh y se redondea a **4** decimales.
    4. Redondea promedio y máximo a **2** decimales.
    5. No ordena ni rellena períodos: el repositorio ya viene ordenado y sin huecos.
  * **`Etiquetar(periodoInicio, granularidad)`** (privado, `switch`): `DIA` → `dd/MM/yyyy`; `MES` → `"<mes> <año>"` con el arreglo `Meses` en minúsculas («octubre 2026»); `ANIO` → el año. Un valor no previsto cae en `dd/MM/yyyy`.
  * **Límite conocido y aceptado:** si el colector estuvo detenido o perdió rondas, hay menos lecturas y la energía estimada queda **por debajo** de la real. El sistema no lo detecta ni lo advierte (ver Preguntas abiertas).
* No hay servicio de escritura: la funcionalidad es de solo lectura y no valida datos de entrada (los `DateTime?` ya vienen tipados desde el formulario).

## 3. Capa Infrastructure (`SolarUY.Infrastructure`)
* **`TelemetriaRepository.ObtenerAgregadoPorPanelAsync`**:
  * Consulta `LecturasTelemetria` con `AsNoTracking` y filtra por `PanelSolarId`.
  * Si `desde` no es `null`, agrega `FechaHora >= desde`; si `hasta` no es `null`, `FechaHora <= hasta`. Los filtros son acumulativos.
  * Agrupa con `GroupBy` y proyecta `Count`, `Average(PotenciaGeneradaWatts)`, `Max(...)` y `Sum(...)`: **todo se calcula en SQL**, no en memoria (evita traer ~260.000 lecturas por día y panel).
  * La clave de agrupación es de **enteros** (`{ Year, Month, Day }`, `{ Year, Month }` o `Year`), no un `DateTime` recortado: construir el `DateTime` dentro de la consulta no siempre se traduce a SQL, así que el período (`PeriodoInicio`, primer instante del día, del mes o del año) se arma después, del lado del cliente.
  * `granularidad` distinta de `DIA` o `MES` cae en la rama de año (`default`). Es el mismo comportamiento que el enum: solo hay tres valores.
  * Ordena el resultado por `PeriodoInicio` descendente.
* **EF Core y esquema:** sin entidad nueva, sin configuración nueva y **sin migración**. Compatible con la versión anterior de la aplicación: es una consulta nueva sobre una tabla existente.
* **Índices:** usa el índice `(panel_solar_id, fecha_hora)` que ya existe desde `req005`, útil para el filtro por panel y rango. La suma y el agrupamiento no se pueden resolver solo con ese índice (la clave de agrupación es una expresión sobre `fecha_hora`), así que el motor lee las filas del rango. Si el volumen crece, la opción es un índice que además cubra `potencia_generada_watts` (ver `tasks.md`).

## 4. Capa Presentation (`SolarUY.Web`)
* **`HistoricoController`** (`[Authorize(Roles = "Administrador")]`):
  * `GET /Historico` → **`Index(Guid? panelId, GranularidadConsulta granularidad = DIA, DateTime? desde, DateTime? hasta, CancellationToken ct)`**. Es la única acción; no hay POST ni acciones de escritura.
  * Trae los paneles con `PanelService.ObtenerTodosAsync(ct)` para llenar el combo (todos, sin filtrar por estado ni por nodo).
  * **Panel propuesto:** `panelId ?? paneles.FirstOrDefault()?.Id`. A diferencia de Mantenimiento, acá «todos» no tiene sentido: sin panel un elegido la pantalla no tendría nada que mostrar.
  * **Fin de día de `hasta`:** antes de llamar al servicio convierte la fecha a `hasta.Value.Date.AddDays(1).AddTicks(-1)`. `desde` viaja tal cual, porque el `<input type="date">` manda la medianoche del día elegido.
  * Si no hay paneles, no consulta y deja `Resultados` vacío.
  * Arma el `HistoricoIndexViewModel` y devuelve la vista. No hay mensajes de error propios: los avisos los pone la vista.
* **`HistoricoIndexViewModel`** (`Models/`): `Paneles` (`IReadOnlyList<PanelDto>`), `PanelSolarId` (`Guid?`), `Granularidad` (`GranularidadConsulta`, por defecto `DIA`), `Desde` y `Hasta` (`DateTime?`, ambos con `[DataType(DataType.Date)]` para que el navegador muestre el selector de fecha) y `Resultados` (`IReadOnlyList<ResumenGeneracionDto>`). **Sin validaciones**: no hay POST que validar.
* **`Views/Historico/Index.cshtml`**:
  * Formulario `GET` a la misma acción con: combo de paneles («código (nodo)»), combo «Agrupar por» generado con `Enum.GetValues<GranularidadConsulta>()` (muestra `DIA`, `MES`, `ANIO`), «Desde», «Hasta», botón **Consultar** y enlace **Limpiar** (`asp-action="Index"`, sin parámetros, que vuelve al panel propuesto por defecto).
  * Leyenda con código, modelo, nodo y potencia nominal del panel elegido, para saber sobre qué se está consultando.
  * Aviso fijo y en texto chico de que la energía **es una estimación** y de que se asume una lectura cada `ReporteService.IntervaloLecturaSegundos` segundos.
  * Tabla con las cinco columnas pedidas; energías con 4 decimales y potencias con 2. `tfoot` con «Total»: suma de lecturas y suma de energía. Promedio y máximo quedan en blanco porque sumarlos no significa nada.
  * Avisos: lista de paneles vacía → «Todavía no hay paneles cargados» (y no se muestra el formulario); sin resultados → «No hay lecturas para este panel con estos filtros».
* **Menú y usings:** enlace «Histórico» en `Views/Shared/_Layout.cshtml` y `@using SolarUY.Application.Services` en `Views/_ViewImports.cshtml` (la vista usa la constante de `ReporteService`).

## 5. Trazabilidad

| Criterio | Dónde se cumple |
|---|---|
| 1 | `GranularidadConsulta` + combo «Agrupar por» de `Index.cshtml` + valor por defecto de `Index` |
| 2 | `desde`/`hasta` opcionales en `ITelemetriaRepository` y en `HistoricoController.Index`; `null` = sin filtro |
| 3 | `HistoricoController.Index`: `hasta.Value.Date.AddDays(1).AddTicks(-1)` |
| 4 | `HistoricoController.Index`: `panelId ?? paneles.FirstOrDefault()?.Id` |
| 5 | `ResumenGeneracionDto` + tabla de `Index.cshtml` |
| 6 | `ReporteService.Etiquetar` + `OrderByDescending(PeriodoInicio)` en el repositorio |
| 7 | `ReporteService.IntervaloLecturaSegundos = 5` + cálculo de `EnergiaEstimadaKWh` + aviso de la vista |
| 8 | `GroupBy` sobre las lecturas existentes: no hay períodos sin fila |
| 9 | `tfoot` de `Index.cshtml` (`Sum` de lecturas y de energía) |
| 10 | `GroupBy` + `Average`/`Max`/`Sum`/`Count` traducidos a SQL en `TelemetriaRepository` |
| 11 | `HistoricoController` (sin paneles no consulta) + dos avisos de `Index.cshtml` |
| 12 | `[Authorize(Roles = "Administrador")]` + enlace en `_Layout.cshtml` |
