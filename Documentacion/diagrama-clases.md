# Diagrama de clases — SolarUY

Modelo de dominio vigente, verificado contra el código de `src/SolarUY.Domain` (14/10/2026).
El código Mermaid de abajo es la versión actualizada: se puede pegar en
[mermaid.live](https://mermaid.live) o renderizar con la extensión de Mermaid de VS Code.

```mermaid
classDiagram
    %% ===== Dominio principal: parque solar =====
    class ParqueSolar {
        +Guid Id
        +string Nombre
        +string Ubicacion
        +double CapacidadInstaladaKW
        +DateTime FechaInstalacion
        +List~Nodo~ Nodos
        +Actualizar(nombre, ubicacion, capacidadInstaladaKW, fechaInstalacion) void
        +TieneNodos() bool
    }

    class Nodo {
        +Guid Id
        +string Nombre
        +string UbicacionDentroParque
        +Guid ParqueSolarId
        +Inversor Inversor
        +List~PanelSolar~ Paneles
        +double TemperaturaMaximaCelsius
        +double UmbralRendimientoBajo
        +double UmbralFallaDetectada
        +double RendimientoMinimoParaEvaluarCaida
        +Actualizar(nombre, ubicacionDentroParque, parqueSolarId) void
        +TienePaneles() bool
        +BuscarPanelPorCodigo(codigoPanel) PanelSolar
        +EvaluarLecturas(lecturas) IReadOnlyList~LecturaTelemetria~
    }

    class Inversor {
        +Guid Id
        +string Modelo
        +string NumeroSerie
        +double EficienciaConversion
        +bool VolcadoRedActivo
        +Guid NodoId
        +SensorDispositivo Sensor
        +Actualizar(modelo, numeroSerie, eficienciaConversion, volcadoRedActivo) void
        +RegistrarSensor(sensor) void
        +QuitarSensor() void
        +EstablecerVolcadoRed(activo) void
    }

    class PanelSolar {
        +Guid Id
        +string CodigoPanel
        +string Modelo
        +string Fabricante
        +double PotenciaNominalW
        +bool Activo
        +DateTime FechaInstalacion
        +EstadoSalud Estado
        +Guid NodoId
        +List~LecturaTelemetria~ Lecturas
        +List~TareaMantenimiento~ Mantenimientos
        +Actualizar(codigoPanel, modelo, fabricante, potenciaNominalW, estado, fechaInstalacion, nodoId, activo) void
        +MarcarActividad(activo) void
        +CambiarEstado(estado) void
        +CodigoValido(codigo) bool
    }

    %% ===== Sensor y telemetría =====
    class SensorDispositivo {
        +Guid Id
        +string IdentificadorHardwareARM
        +string VersionFirmware
        +string UrlBase
        +bool Activo
        +Guid InversorId
        +Actualizar(identificadorHardwareARM, versionFirmware, activo, urlBase) void
        +Activar() void
        +Desactivar() void
        +DireccionValida(urlBase) bool
    }

    class LecturaTelemetria {
        +Guid Id
        +Guid PanelSolarId
        +DateTime FechaHora
        +double CorrienteContinua
        +double Voltaje
        +double PotenciaGeneradaWatts
        +double TemperaturaCelsius
        +EstadoSalud EstadoDetectado
    }

    %% ===== Mantenimiento =====
    class TareaMantenimiento {
        +Guid Id
        +Guid PanelSolarId
        +TipoMantenimiento Tipo
        +DateTime FechaProgramada
        +DateTime FechaRealizada
        +string Descripcion
        +string PersonalAsignado
        +EstadoMantenimiento Estado
        +Reprogramar(fechaProgramada, descripcion, personalAsignado) void
        +Iniciar() void
        +Completar(fechaRealizada) void
        +Cancelar() void
    }

    %% ===== Enumeraciones =====
    class EstadoSalud {
        <<enumeration>>
        OPERATIVO
        RENDIMIENTO_BAJO
        FALLA_DETECTADA
        DESCONECTADO
    }

    class TipoMantenimiento {
        <<enumeration>>
        LIMPIEZA
        REPARACION
        INSPECCION_PREVENTIVA
        REEMPLAZO
    }

    class EstadoMantenimiento {
        <<enumeration>>
        PENDIENTE
        EN_PROCESO
        COMPLETADO
        CANCELADO
    }

    %% ===== Relaciones =====
    ParqueSolar "1" *-- "1..*" Nodo : contiene
    Nodo "1" *-- "1" Inversor : equipado con
    Nodo "1" *-- "1..*" PanelSolar : agrupa
    Inversor "1" *-- "0..1" SensorDispositivo : acoplado con
    PanelSolar "1" o-- "0..*" TareaMantenimiento : requiere
    PanelSolar "1" --> "0..*" LecturaTelemetria : registra
    Nodo ..> EstadoSalud : diagnostica
    PanelSolar ..> EstadoSalud : utiliza
    LecturaTelemetria ..> EstadoSalud : EstadoDetectado
    TareaMantenimiento ..> TipoMantenimiento : clasifica
    TareaMantenimiento ..> EstadoMantenimiento : estado

    note for SensorDispositivo "Caja negra por HTTP: no genera lecturas.\nEl colector lo consulta con ISensorClient.ObtenerEstadoAsync(urlBase, identificadorHardwareARM)\ny arma las LecturaTelemetria, que Nodo.EvaluarLecturas diagnostica."
    note for Nodo "Regla de estado (spec del simulador 9): inactivo = DESCONECTADO;\n> 75 C = FALLA_DETECTADA; caida de potencia contra el promedio de los\npaneles hermanos activos: hasta 15 % OPERATIVO, hasta 40 % RENDIMIENTO_BAJO,\nmas de 40 % FALLA_DETECTADA."
```

## Cambios respecto de la versión anterior del diagrama

1. **`PanelSolar`**: se agregaron `CodigoPanel` (el vínculo con el panel que informa el sensor, único
   dentro del nodo) y `Activo` (habilita o no el panel en el hardware), más `MarcarActividad`,
   `CambiarEstado` y `CodigoValido`. Los dos campos llegaron en la migración
   `VinculacionConElSensor`.
2. **`SensorDispositivo`**: se agregó `UrlBase`, la dirección donde escucha el sensor, que es lo que
   permite consultarlo por HTTP. Se quitaron `CapturarDatosInversor()` y `EjecutarOrden()`: el sensor no
   genera lecturas en nuestro modelo (es una caja negra que se consulta con
   `ISensorClient.ObtenerEstadoAsync`) y las órdenes viven en el contrato de aplicación
   (`ActivarPanelAsync`, `DesactivarPanelAsync`, `ActivarVolcadoAsync`, `DesactivarVolcadoAsync`),
   todavía sin cablear a la interfaz.
3. **`Nodo`**: se quitó `GetEstadoSaludGeneral()`. Lo que hay es `EvaluarLecturas(lecturas)`, que
   diagnostica las lecturas de todos los paneles del nodo y devuelve el estado de cada una, más
   `BuscarPanelPorCodigo`.
4. **`Inversor`**: se quitó `ConvertirCCaCA()` (la potencia la informa el sensor ya convertida). Se
   agregaron `RegistrarSensor`, `QuitarSensor` y `EstablecerVolcadoRed`. La relación con el sensor pasó a
   **0..1**: el dominio admite un inversor sin sensor (el ABM lo permite y avisa).
5. **`TareaMantenimiento`**: `CompletarTarea(observaciones)` no existe; el método es
   `Completar(fechaRealizada)` y se sumaron `Reprogramar`, `Iniciar` y `Cancelar`. `PersonalAsignado` es
   opcional (`string?`).
6. **`ParqueSolar`**: se quitó `CalcularGeneracionTotalActual()`: requiere agregar lecturas y pertenece
   al hito de Monitoreo. Hoy el parque expone `TieneNodos()`.
7. **Relación `SensorDispositivo ..> LecturaTelemetria`** ("genera"): reemplazada por una nota, porque el
   sensor sólo mide. El camino real es sensor → `ISensorClient` → colector → `Nodo.EvaluarLecturas` →
   `LecturaTelemetria` guardada.
8. Los **enums** ya estaban correctos (`EstadoSalud`, `TipoMantenimiento`, `EstadoMantenimiento`).

## Correspondencia con la base de datos

| Clase | Tabla |
|---|---|
| `ParqueSolar` | `parques_solares` |
| `Nodo` | `nodos` |
| `Inversor` | `inversores` |
| `SensorDispositivo` | `sensores_dispositivos` |
| `PanelSolar` | `paneles` |
| `LecturaTelemetria` | `lecturas_telemetria` |
| `TareaMantenimiento` | `mantenimientos` |

Las reglas que el dominio hace cumplir (unicidad de número de serie y de identificador de hardware,
código de panel único por nodo, un inversor por nodo, no borrar un parque con nodos ni un nodo con
paneles) están además respaldadas con índices y claves foráneas en las migraciones de
`src/SolarUY.Infrastructure/Persistence/Migrations`.
