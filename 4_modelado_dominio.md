# Modelado del Dominio - SolarUY

## Entidades y Atributos Principales
- **ParqueSolar:** `Id`, `Nombre`, `Ubicacion`, `CapacidadInstaladaKW`, `FechaInstalacion`, `List<Nodo>`.
- **Nodo:** `Id`, `Nombre`, `UbicacionDentroParque`, `ParqueSolarId`, `Inversor`, `List<PanelSolar>`.
- **Inversor:** `Id`, `Modelo`, `NumeroSerie`, `EficienciaConversion`, `VolcadoRedActivo`, `NodoId`, `SensorDispositivo`.
- **PanelSolar:** `Id`, `Modelo`, `Fabricante`, `PotenciaNominalW`, `EstadoSalud`, `NodoId`, `List<TareaMantenimiento>`.
- **SensorDispositivo:** `Id`, `IdentificadorHardwareARM`, `VersionFirmware`, `Activo`, `InversorId`.
- **LecturaTelemetria:** `Id`, `PanelSolarId`, `FechaHora`, `CorrienteContinua`, `Voltaje`, `PotenciaGeneradaWatts`, `TemperaturaCelsius`, `EstadoDetectado`.
- **TareaMantenimiento:** `Id`, `PanelSolarId`, `TipoMantenimiento`, `FechaProgramada`, `FechaRealizada`, `Descripcion`, `EstadoMantenimiento`.

## Reglas de Negocio
1. Un Nodo debe tener exactamente un Inversor asignado.
2. Solo usuarios con rol `Administrador` pueden crear/modificar Parques, Nodos y Paneles.
3. Si la temperatura de un Panel supera los 75°C o la potencia cae más del 40%, el sensor debe emitir un estado `FALLA_DETECTADA`.
