---
trigger: always_on
---

# Modelado del Dominio - SolarUY

## Entidades y Atributos Principales

* **ParqueSolar:** `Id`, `Nombre`, `Ubicacion`, `CapacidadInstaladaKW`, `FechaInstalacion`, `List<Nodo> Nodos`.
* **Nodo:** `Id`, `Nombre`, `UbicacionDentroParque`, `ParqueSolarId`, `ParqueSolar`, `Inversor`, `List<PanelSolar> Paneles`.
* **Inversor:** `Id`, `Modelo`, `NumeroSerie`, `EficienciaConversion`, `VolcadoRedActivo`, `NodoId`, `Nodo`, `SensorDispositivo Sensor`.
* **PanelSolar:** `Id`, `Modelo`, `Fabricante`, `PotenciaNominalW`, `FechaInstalacion`, `EstadoSalud Estado`, `NodoId`, `Nodo`, `List<TareaMantenimiento> Mantenimientos`.
* **SensorDispositivo:** `Id`, `IdentificadorHardwareARM`, `VersionFirmware`, `Activo`, `InversorId`.
* **LecturaTelemetria:** `Id`, `PanelSolarId`, `FechaHora`, `CorrienteContinua`, `Voltaje`, `PotenciaGeneradaWatts`, `TemperaturaCelsius`, `EstadoSalud EstadoDetectado`.
* **TareaMantenimiento:** `Id`, `PanelSolarId`, `TipoMantenimiento Tipo`, `FechaProgramada`, `FechaRealizada`, `Descripcion`, `PersonalAsignado`, `EstadoMantenimiento Estado`.

## Comportamiento previsto en el diagrama de clases

Estos métodos pertenecen a las entidades. No reimplementar su lógica en servicios ni en controladores:

* `ParqueSolar.CalcularGeneracionTotalActual()`
* `Nodo.GetEstadoSaludGeneral()`
* `Inversor.ConvertirCCaCA(double corrienteCC)`
* `TareaMantenimiento.CompletarTarea(string observaciones)`

## Reglas de Negocio

1. Un Nodo debe tener exactamente un Inversor asignado.
2. Solo usuarios con rol `Administrador` pueden crear/modificar Parques, Nodos y Paneles.
3. Si la temperatura de un Panel supera los 75 °C o la potencia cae más del 40 % respecto a la esperada, la lectura se clasifica como `FALLA_DETECTADA`. Los umbrales (75 °C y 40 %) se definen como constantes con nombre, no como números sueltos en el código.
4. No pueden existir dos parques con el mismo nombre.
5. Una tarea de mantenimiento `COMPLETADO` o `CANCELADO` no puede volver a otro estado.

## Cómo modelar

* **Objetos, no tablas.** Las entidades se modelan pensando en comportamiento y en el lenguaje del negocio, no como un reflejo de las tablas de MySQL.
* **Relaciones ricas.** Toda clave foránea (`ParqueSolarId`, `NodoId`, `PanelSolarId`, `InversorId`) va acompañada de su propiedad de navegación (`ParqueSolar`, `Nodo`, ...). Nunca una clave foránea sola.
* **Comportamiento adentro de la entidad.** Si la lógica que cambia el estado de una entidad (ej. completar una tarea, clasificar una lectura) queda en un servicio y la entidad solo tiene `get/set`, el agente lo advierte y sugiere moverla a la entidad (evitar el Dominio Anémico).
* **Setters públicos permitidos** para facilitar el trabajo con EF Core. Pero si un cambio requiere validaciones, se encapsula en un método de la entidad.
* **Invariantes.** Una entidad no puede quedar en un estado inválido: nombre vacío, potencia negativa o cambiar una tarea ya cerrada lanzan una excepción de dominio. Las validaciones de formato del formulario son otra cosa y van en el ViewModel o en FluentValidation.
* **Identidad.** Los `Id` son `Guid` y los genera el sistema al crear la entidad. Nunca se reciben de un formulario.
* **Estados.** Siempre con los enums del diagrama, nunca con strings o números sueltos.

## Lecturas de Telemetría

`LecturaTelemetria` crece sin límite. Nunca cargar la colección completa de lecturas de un panel (por ejemplo con `Include`): consultarlas siempre filtradas por rango de fechas y, para el histórico, agregadas por día, mes o año en la propia consulta.
