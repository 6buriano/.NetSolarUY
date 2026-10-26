---
trigger: always_on
---

# Modelado del Dominio - SolarUY

## Entidades y Atributos Principales

El diagrama de clases vigente está en `Documentacion/diagrama-clases.md`. Resumen:

* **ParqueSolar:** `Id`, `Nombre`, `Ubicacion`, `CapacidadInstaladaKW`, `FechaInstalacion`, `List<Nodo> Nodos`.
* **Nodo:** `Id`, `Nombre`, `UbicacionDentroParque`, `ParqueSolarId`, `Inversor Inversor`, `List<PanelSolar> Paneles`.
* **Inversor:** `Id`, `Modelo`, `NumeroSerie`, `EficienciaConversion`, `VolcadoRedActivo`, `NodoId`, `SensorDispositivo? Sensor` (0 o 1).
* **PanelSolar:** `Id`, `CodigoPanel` (único dentro del nodo: lo vincula con el panel del sensor), `Modelo`, `Fabricante`, `PotenciaNominalW`, `Activo`, `FechaInstalacion`, `EstadoSalud Estado`, `NodoId`, `List<LecturaTelemetria> Lecturas`, `List<TareaMantenimiento> Mantenimientos`.
* **SensorDispositivo:** `Id`, `IdentificadorHardwareARM`, `VersionFirmware`, `UrlBase`, `Activo`, `InversorId`.
* **LecturaTelemetria:** `Id`, `PanelSolarId`, `FechaHora`, `CorrienteContinua`, `Voltaje`, `PotenciaGeneradaWatts`, `TemperaturaCelsius`, `EstadoSalud EstadoDetectado`.
* **TareaMantenimiento:** `Id`, `PanelSolarId`, `TipoMantenimiento Tipo`, `FechaProgramada`, `FechaRealizada?`, `Descripcion`, `PersonalAsignado?`, `EstadoMantenimiento Estado`.

## Comportamiento que ya vive en las entidades

No reimplementar esta lógica en servicios ni en controladores: se llama al método de la entidad.

* `ParqueSolar.Actualizar(...)`, `ParqueSolar.TieneNodos()`.
* `Nodo.Actualizar(...)`, `Nodo.TienePaneles()`, `Nodo.BuscarPanelPorCodigo(codigo)` y **`Nodo.EvaluarLecturas(lecturas)`** (diagnóstico del estado de cada panel).
* `Inversor.RegistrarSensor(...)`, `Inversor.QuitarSensor()`, `Inversor.EstablecerVolcadoRed(...)`.
* `PanelSolar.MarcarActividad(...)`, `PanelSolar.CambiarEstado(...)`.
* `TareaMantenimiento.Reprogramar(...)`, `Iniciar()`, `Completar(fecha)`, `Cancelar()`.

## Reglas de Negocio

1. Un nodo debe tener exactamente un inversor. El sensor del inversor es opcional (0 o 1).
2. El número de serie del inversor y el identificador de hardware del sensor son únicos en todo el sistema.
3. Un parque con nodos no se puede eliminar, y un nodo con paneles tampoco.
4. El código de panel es único dentro de su nodo.
5. **Estado de un panel** (lo decide `Nodo.EvaluarLecturas`, *el sensor mide, el dominio decide*):
   * panel inactivo → `DESCONECTADO`;
   * temperatura mayor a 75 °C → `FALLA_DETECTADA`;
   * caída de rendimiento respecto del **promedio de los otros paneles activos del nodo** (normalizada por potencia nominal): hasta 15 % → `OPERATIVO`; más de 15 % y hasta 40 % → `RENDIMIENTO_BAJO`; más de 40 % → `FALLA_DETECTADA`;
   * de noche o con menos de dos paneles activos con lectura, solo se evalúa la temperatura.
   Los umbrales se definen como constantes con nombre, no como números sueltos en el código.
6. Solo usuarios con rol `Administrador` pueden crear o modificar parques, nodos y paneles (se aplica en `Web` con `[Authorize]`, no en el dominio).
7. Propuestas pendientes de implementar (`prompting/req001_abm_parque_solar`): nombre de parque único y fecha de instalación no futura.
8. Una tarea de mantenimiento `COMPLETADO` o `CANCELADO` no puede volver a otro estado.

## Cómo modelar

* **Objetos, no tablas.** Las entidades se modelan pensando en comportamiento y en el lenguaje del negocio, no como un reflejo de las tablas de MySQL.
* **Encapsulamiento.** Las propiedades son `{ get; private set; }`. El estado solo cambia por el constructor o por métodos de la entidad que validan. Las colecciones se modifican desde la entidad dueña.
* **Constructor privado sin parámetros** solo para EF Core. El constructor público exige todos los datos obligatorios.
* **Comportamiento adentro de la entidad.** Si la lógica que cambia el estado de una entidad queda en un servicio y la entidad solo tiene `get/set`, el agente lo advierte y sugiere moverla a la entidad (evitar el dominio anémico).
* **Invariantes.** Una entidad no puede quedar en un estado inválido: los métodos lanzan `ReglaDeNegocioException` con un mensaje en español para el usuario. Las validaciones de formato del formulario son otra cosa y van en el ViewModel (DataAnnotations).
* **Reglas que necesitan la base** (ej. "el nombre no se repite") no van en la entidad, que no conoce la base: van en el servicio de `Application`, con un método del repositorio.
* **Identidad.** Los `Id` son `Guid` y los genera la entidad al construirse (`Guid.NewGuid()`). Nunca se reciben de un formulario de alta.
* **Estados.** Siempre con los enums del diagrama, nunca con strings o números sueltos.

## Lecturas de Telemetría

`LecturaTelemetria` crece sin límite (unas 260.000 filas por día con 15 paneles). Nunca cargar la colección completa de lecturas de un panel (por ejemplo con `Include`): consultarlas siempre filtradas por rango de fechas o con un límite, y, para el histórico, agregadas por día, mes o año en la propia consulta.
