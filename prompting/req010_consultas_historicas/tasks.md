# Tareas de Implementación: Consultas históricas de generación

> Documentado después de implementar: las tareas reflejan lo que ya está en `main` (commit «Consultas históricas»).

- [x] **1. Domain**
  - [x] 1.1 `GranularidadConsulta` (`DIA`, `MES`, `ANIO`) en `Enums/`.
  - [x] 1.2 `AgregadoPotencia` (`record`) en `Repositories/` con período, cantidad, promedio, máximo y suma.
  - [x] 1.3 `ObtenerAgregadoPorPanelAsync` en `ITelemetriaRepository`, con rango opcional y orden del más reciente al más antiguo.
  - [x] 1.4 Sin tests de dominio: el requerimiento no agrega comportamiento de entidad (el cálculo queda en `ReporteService`, ver 6.1).

- [x] **2. Infrastructure**
  - [x] 2.1 `TelemetriaRepository.ObtenerAgregadoPorPanelAsync`: consulta base con `AsNoTracking` y filtro por panel.
  - [x] 2.2 Filtros opcionales por `desde` y `hasta`.
  - [x] 2.3 Ramas de agrupación por día, mes y año, con `Count`/`Average`/`Max`/`Sum` en SQL.
  - [x] 2.4 Armado de `PeriodoInicio` del lado del cliente, a partir de la clave entera del grupo.
  - [x] 2.5 Orden descendente por `PeriodoInicio`.
  - [x] 2.6 Sin migración: reutiliza la tabla y el índice `(panel_solar_id, fecha_hora)` de `req005`.

- [x] **3. Application**
  - [x] 3.1 `ResumenGeneracionDto` con etiqueta y energía.
  - [x] 3.2 `ReporteService.IntervaloLecturaSegundos = 5`, documentado como supuesto de la estimación.
  - [x] 3.3 `ReporteService.ObtenerGeneracionPorPanelAsync`: energía en kWh y redondeos (4 y 2 decimales).
  - [x] 3.4 `ReporteService.Etiquetar` y el arreglo `Meses` en español.

- [x] **4. Registro de dependencias**
  - [x] 4.1 `builder.Services.AddScoped<ReporteService>()` en `src/SolarUY.Web/Program.cs`.

- [x] **5. Presentation**
  - [x] 5.1 `HistoricoIndexViewModel` (`Paneles`, `PanelSolarId`, `Granularidad`, `Desde`, `Hasta`, `Resultados`).
  - [x] 5.2 `HistoricoController.Index` con `[Authorize(Roles = "Administrador")]` y panel propuesto por defecto.
  - [x] 5.3 Fin de día de `hasta` (`Date.AddDays(1).AddTicks(-1)`) antes de llamar al servicio.
  - [x] 5.4 `Views/Historico/Index.cshtml`: formulario `GET` con panel, granularidad, desde, hasta, Consultar y Limpiar.
  - [x] 5.5 Leyenda del panel elegido y aviso de que la energía es estimada, leyendo la constante del servicio.
  - [x] 5.6 Tabla de cinco columnas con `tfoot` de totales (lecturas y energía).
  - [x] 5.7 Avisos de lista de paneles vacía y de consulta sin resultados.
  - [x] 5.8 Enlace «Histórico» en `Views/Shared/_Layout.cshtml` y `@using SolarUY.Application.Services` en `Views/_ViewImports.cshtml`.

- [x] **6. Verificación**
  - [x] 6.1 `dotnet build` y `dotnet test` en verde. *(confirmar con quien lo implementó)*
  - [x] 6.2 Prueba manual: consultar las tres granularidades de un panel con lecturas y comparar etiquetas, cantidad de filas y totales contra la tabla de la base. *(confirmar con quien lo implementó)*
  - [x] 6.3 Prueba manual: `hasta = hoy` incluye las lecturas del día de hoy. *(confirmar con quien lo implementó)*
  - [x] 6.4 Prueba manual: entrar sin `panelId` propone el primero; un panel sin lecturas y un parque sin paneles muestran su aviso. *(confirmar con quien lo implementó)*
  - [x] 6.5 Prueba manual: un usuario sin rol Administrador no accede a `/Historico`.

- [ ] **7. Pendiente**
  - [ ] 7.1 Tests de `ReporteService`: energía en kWh, redondeos y las tres etiquetas (se pueden probar sin base).
  - [ ] 7.2 Pasar `verificar-calidad` (ArchUnit + SonarQube) cuando estén integrados.
  - [ ] 7.3 Evaluar un índice que cubra la consulta de agregados (`panel_solar_id`, `fecha_hora`, `potencia_generada_watts`): hoy el motor lee las filas del rango para sumar la potencia.
  - [ ] 7.4 Responder las preguntas abiertas de `specs.md` (aviso cuando el período tiene menos lecturas de las esperadas).

## Uso de IA
- **Código:** implementado en la iteración 1 con asistencia de IA, sin documentos SDD previos.
- **Este requerimiento, `specs.md` y `tasks.md`:** reconstruidos con asistencia de IA leyendo el código de `main`, y revisados por el equipo. *(Completar: quién lo revisó.)*
