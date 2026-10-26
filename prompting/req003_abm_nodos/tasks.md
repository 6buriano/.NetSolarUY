# Tareas de Implementación: ABM de Nodos

> Documentado después de implementar: las tareas reflejan lo que ya está en `main`.

- [x] **1. Domain**
  - [x] 1.1 Entidades `Nodo`, `Inversor` y `SensorDispositivo` con `private set` y validaciones.
  - [x] 1.2 `Nodo` exige `Inversor` en el constructor.
  - [x] 1.3 Métodos `Actualizar`, `TienePaneles`, `BuscarPanelPorCodigo`, `RegistrarSensor`, `QuitarSensor`.
  - [x] 1.4 `INodoRepository` con las consultas de unicidad (`ExisteNumeroSerieInversorAsync`, `ExisteIdentificadorHardwareAsync`).
  - [x] 1.5 Tests: `NodoTests`, `InversorTests`, `SensorDispositivoTests`.

- [x] **2. Application**
  - [x] 2.1 DTOs y requests (`NodoDto`, `InversorDto`, `SensorDispositivoDto`, `CrearNodoRequest`, `ActualizarNodoRequest`, `InversorRequest`, `SensorDispositivoRequest`).
  - [x] 2.2 `NodoService` (crear, editar con sensor opcional, eliminar con la regla de paneles).
  - [x] 2.3 `ISensorDisponibilidadService` con implementación del simulador y `SensorDisponibilidadNula`.

- [x] **3. Infrastructure**
  - [x] 3.1 Configuración EF: índice único del número de serie y cascadas.
  - [x] 3.2 `NodoRepository` y registro en `AddInfrastructure`.
  - [x] 3.3 Migración.

- [x] **4. Web**
  - [x] 4.1 `NodoFormViewModel` (`IValidatableObject`).
  - [x] 4.2 `NodosController` con el flujo de sensor inexistente (`CrearEnHardware` / `SinSensor`).
  - [x] 4.3 Vistas y `_Formulario.cshtml`.
  - [x] 4.4 Elección de `ISensorDisponibilidadService` por entorno en `Program.cs`.

- [x] **5. Verificación**
  - [x] 5.1 `dotnet build` y `dotnet test` en verde.
  - [x] 5.2 Prueba manual: alta con sensor existente, con sensor inexistente (las dos salidas), edición quitando el sensor, baja de un nodo con paneles (bloqueada). *(confirmar con quien lo implementó)*

- [ ] **6. Pendiente**
  - [ ] 6.1 Pasar la verificación de calidad (`verificar-calidad`: ArchUnit + SonarQube) cuando estén integrados.
  - [ ] 6.2 Decidir si se agrega un índice único al identificador ARM del sensor (hoy solo lo controla el servicio).

## Uso de IA
- **Código:** implementado en la iteración 1 con asistencia de IA (Antigravity), sin documentos SDD previos.
- **Este requerimiento, `specs.md` y `tasks.md`:** reconstruidos con asistencia de IA leyendo el código de `main`, y revisados por el equipo. *(Completar: quién lo revisó.)*
