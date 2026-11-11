# Tareas de Implementación: Cierre de las tareas de mantenimiento

> Un commit por tarea (`req013 N.M: …`), compilando antes de cada uno.

- [x] **1. Domain**
  - [x] 1.1 `TareaMantenimiento.ObservacionesCierre` y `LargoMaximoObservaciones` (500).
  - [x] 1.2 `Completar(fechaRealizada, observaciones, ahoraUtc)`: estado, fecha obligatoria, fecha no futura,
        largo de las observaciones y recorte. Guarda las dos cosas.
  - [x] 1.3 Tests de dominio: guarda fecha y observaciones, vacías quedan nulas, más de 500 falla, fecha futura
        falla, fecha vacía falla, transiciones inválidas siguen fallando.

- [x] **2. Infrastructure**
  - [x] 2.1 `TareaMantenimientoConfiguration`: `observaciones_cierre` (`varchar(500)`, nullable).
  - [x] 2.2 Migración `CierreDeMantenimiento` (sólo `AddColumn`). Aplicada en la base local junto con la del
        índice por `fecha_hora`, que estaba pendiente.

- [x] **3. Application**
  - [x] 3.1 `MantenimientoDto.ObservacionesCierre` y `MantenimientoMappings`.
  - [x] 3.2 `MantenimientoService`: `TimeProvider` en el constructor y `CompletarAsync` con fecha y
        observaciones.

- [x] **4. Presentation**
  - [x] 4.1 `HoraLocal.AParaUtc` (camino inverso de `DesdeUtc`).
  - [x] 4.2 `CierreMantenimientoViewModel`.
  - [x] 4.3 `MantenimientosController.Completar` con el modelo, la conversión a UTC y los errores de regla.
  - [x] 4.4 Detalle: formulario de cierre (fecha propuesta en hora de Montevideo + observaciones) y las
        observaciones en el detalle. De paso, la fecha realizada se muestra en hora de Montevideo (antes salía
        en UTC).

- [x] **5. Documentación**
  - [x] 5.1 `Documentacion/DOCUMENTACION-SolarUY.md`: §4.3 (regla 7), §5.4, §6.6 (devolución del docente),
        anexo B.2 (columna nueva) y el conteo de pruebas.
  - [x] 5.2 `Documentacion/diagrama-clases.md` (campo y firma nuevos).
  - [x] 5.3 `README.md`: la fila de mantenimiento y las migraciones.

- [x] **6. Verificación**

  - [x] 6.1 `dotnet build`: 0 advertencias, 0 errores.
  - [x] 6.2 `dotnet test`: 216 pruebas en verde (ver el detalle en la sección siguiente).
  - [x] 6.3 Migraciones aplicadas en la base local: `observaciones_cierre` e `IX_lecturas_telemetria_fecha_hora`.
  - [ ] 6.4 Prueba manual del formulario de cierre en la pantalla (queda para la verificación del equipo).

## Uso de IA

- **Origen:** devolución del docente de la segunda entrega. Los cuatro puntos se mapearon contra el código
  antes de escribir nada: uno (las lecturas en vivo) ya estaba resuelto en el `req012`.
- **Documentos SDD:** `requirements.md` y `specs.md` se escribieron antes del código.
- **Código:** generado con asistencia de IA y revisado por quien lo commiteó, tarea por tarea.
- **Decisiones revisadas:**
  - La hora actual entra a `Completar` como parámetro en lugar de leer el reloj dentro del dominio: la regla
    «la fecha no puede ser futura» queda en el dominio y se prueba con una fecha fija.
  - La fecha del formulario se convierte de hora de Montevideo a UTC en el controlador, con el mismo
    `HoraLocal` que usa el resto de las pantallas, en vez de restar tres horas a mano.
- **Problema encontrado:** la fecha realizada se mostraba en UTC en el detalle (siempre lo hizo, pero no se
  notaba porque la cargaba el servidor). Ahora que la carga el usuario se veía la diferencia, así que se
  pasó a hora de Montevideo con `HoraLocal.DesdeUtc`.
- **Deuda:** falta la prueba manual del formulario y no hay tests de la capa de aplicación para
  `MantenimientoService` (el proyecto de tests existe; el caso queda para el próximo cierre).
