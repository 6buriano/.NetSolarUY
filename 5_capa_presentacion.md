# Capa de Presentación - Web ASP.NET Core MVC & SignalR

## Estructura de Vistas
- `/Views/Parques/`: Gestión CRUD Multiparque.
- `/Views/Monitoreo/`: Dashboard en tiempo real con gráficos dinámicos (Chart.js / ApexCharts + SignalR).
- `/Views/Mantenimiento/`: Calendario y lista de tareas para paneles.
- `/Views/Chat/`: Interfaz para consultas en Lenguaje Natural mediante el agente MCP (Gemini).

## Ejemplo de SDD (Software Design Document / Diagrama de Secuencia de Monitoreo)

```mermaid
sequenceDiagram
    autonumber
    participant SensorARM as Simulador Sensor ARM
    participant DB as MySQL DB
    participant Hub as SignalR TelemetryHub
    participant Browser as Cliente Web (Razor + JS)

    SensorARM->>SensorARM: Genera lectura (Voltaje, CC, Temp)
    SensorARM->>DB: Guarda LecturaTelemetria
    SensorARM->>Hub: Envia evento 'NuevaLectura' (Payload JSON)
    Hub->>Browser: Broadcast mediante WebSocket
    Browser->>Browser: Actualiza gráfico en tiempo real (sin recargar)
