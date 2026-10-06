# Especificaciones Técnicas de Arquitectura: SolarUY

## 1. Visión General & Reglas Arquitectónicas
- **Estilo Arquitectónico:** Monolito Modular guiado por Clean Architecture.
- **Tecnología Core:** .NET Core (ASP.NET Core MVC con motor Razor).
- **Comunicación en Tiempo Real:** SignalR (WebSockets).
- **Persistencia:** Base de Datos MySQL + Entity Framework Core (`Pomelo`).
- **Seguridad:** Provider OIDC externo (Keycloak) con tokens JWT y control de acceso basado en roles (`Administrador`).
- **Simulación Telemétrica:** Worker Service .NET independiente que emula sensores ARM de inversores.
- **Consultas con IA:** Servidor MCP (Model Context Protocol) conectado con el modelo Gemini para procesar preguntas en lenguaje natural.
- **Alta Disponibilidad:** Despliegue multinodo en VPS Oracle Cloud (OCI) detrás de un balanceador de carga (Nginx / YARP) con Sticky Sessions y Redis Backplane para SignalR.

---

## 2. Capa de Dominio (SolarUY.Domain)

Contiene la lógica y las reglas centrales del negocio. No posee dependencias externas ni conoce los frameworks.

### 2.1. Entidades Principales
- **ParqueSolar:** `Guid Id`, `string Nombre`, `string Ubicacion`, `double CapacidadInstaladaKW`, `DateTime FechaInstalacion`, `List<Nodo> Nodos`.
- **Nodo:** `Guid Id`, `string Nombre`, `string UbicacionDentroParque`, `Guid ParqueSolarId`, `Inversor Inversor`, `List<PanelSolar> Paneles`.
- **Inversor:** `Guid Id`, `string Modelo`, `string NumeroSerie`, `double EficienciaConversion`, `bool VolcadoRedActivo`, `Guid NodoId`, `SensorDispositivo Sensor`.
- **PanelSolar:** `Guid Id`, `string Modelo`, `string Fabricante`, `double PotenciaNominalW`, `EstadoSalud Estado`, `Guid NodoId`, `List<TareaMantenimiento> Mantenimientos`.
- **SensorDispositivo:** `Guid Id`, `string IdentificadorHardwareARM`, `string VersionFirmware`, `bool Activo`, `Guid InversorId`.
- **LecturaTelemetria:** `Guid Id`, `Guid PanelSolarId`, `DateTime FechaHora`, `double CorrienteContinua`, `double Voltaje`, `double PotenciaGeneradaWatts`, `double TemperaturaCelsius`, `EstadoSalud EstadoDetectado`.
- **TareaMantenimiento:** `Guid Id`, `Guid PanelSolarId`, `TipoMantenimiento Tipo`, `DateTime FechaProgramada`, `DateTime? FechaRealizada`, `string Descripcion`, `EstadoMantenimiento Estado`.

### 2.2. Enums
- **EstadoSalud:** `OPERATIVO`, `RENDIMIENTO_BAJO`, `FALLA_DETECTADA`, `DESCONECTADO`.
- **TipoMantenimiento:** `LIMPIEZA`, `REPARACION`, `INSPECCION_PREVENTIVA`, `REEMPLAZO`.
- **EstadoMantenimiento:** `PENDIENTE`, `EN_PROCESO`, `COMPLETADO`, `CANCELADO`.

### 2.3. Contratos de Repositorio (Interfaces)
- `IParqueRepository`, `INodoRepository`, `IPanelRepository`, `ITelemetriaRepository`, `IMantenimientoRepository`.

---

## 3. Capa de Aplicación (SolarUY.Application)

Contiene los casos de uso, orquestación y transformación de datos.

### 3.1. Servicios de Aplicación
- **ParqueService:** Métodos para la gestión multiparque, alta de nodos y vinculación de paneles.
- **MonitoreoService:** Cálculo de métricas de generación por parque, nodo e inversor (diarias, mensuales y anuales).
- **MantenimientoService:** Programación y seguimiento de tareas de limpieza y reparación.

### 3.2. Interfaces de Servicios Externos
- `ISignalRNotificationService`: Abstracción para el envío de métricas telemétricas en tiempo real hacia los clientes Web.
- `IMCPAgentService`: Abstracción para procesar consultas en lenguaje natural mediante el protocolo MCP.

### 3.3. DTOs (Data Transfer Objects)
- `ParqueDto`, `CrearParqueRequest`, `NodoDto`, `PanelDto`, `LecturaTelemetriaDto`, `ConsultaLenguajeNaturalResponse`.

---

## 4. Capa de Infraestructura (SolarUY.Infrastructure)

Detalles concretos de tecnologías externas, bases de datos y servicios.

### 4.1. Persistencia (EF Core + MySQL)
- **DbContext (`SolarUYDbContext`):** Mapeo ORM de las entidades de dominio a tablas MySQL (`parques_solares`, `nodos`, `inversores`, `paneles`, `lecturas_telemetria`, `mantenimientos`).
- Implementación concreta de los repositorios de datos (`ParqueRepository`, `TelemetriaRepository`, etc.).

### 4.2. Autenticación y Autorización (OpenID Connect / Keycloak)
- Configuración del cliente OIDC en ASP.NET Core (`AddAuthentication().AddOpenIdConnect()`).
- Mapeo de Claims JWT para verificar el rol `Administrador` exigido en las operaciones CRUD.

### 4.3. Simulador del Inversor/Sensor (Worker Service)
- Clase `SimuladorInversorWorker` inheriting de `BackgroundService`.
- Tarea periódica que genera datos teleméticos aleatorios y realistas (corriente, voltaje, temperatura) por panel y los guarda en MySQL e invoca a SignalR.

### 4.4. Agente MCP & Gemini
- Servidor local MCP con herramientas expuestas (`ObtenerEstadoParqueTool`, `ObtenerGeneracionHistoricaTool`).
- Integración con la API de Gemini para responder a preguntas del usuario como *“¿cómo está funcionando todo hoy?”*.

---

## 5. Capa de Presentación (SolarUY.Presentation / Web)

Interfaz de usuario e interacción de entrada al sistema.

### 5.1. Controladores ASP.NET Core MVC
- **ParquesController:** Controla las vistas CRUD para Parques, Nodos y Paneles (Protegido con `[Authorize(Roles = "Administrador")]`).
- **MonitoreoController:** Carga la vista del Dashboard de monitoreo en tiempo real.
- **MantenimientoController:** Vista de gestión de tareas de mantenimiento para el personal técnico.
- **ChatController:** Vista e interacción de consultas en Lenguaje Natural con el modelo de IA.

### 5.2. Componentes SignalR
- **`TelemetryHub`:** Hub de WebSockets que transmite las lecturas en tiempo real enviadas por el simulador a los gráficos cliente (Chart.js / ApexCharts).

### 5.3. Vistas Razor (`.cshtml`)
- `Views/Parques/Index.cshtml`, `Views/Parques/Crear.cshtml`.
- `Views/Monitoreo/Dashboard.cshtml`: Dashboard dinámico con JavaScript/SignalR.
- `Views/Chat/Index.cshtml`: Interfaz conversacional para consultas con IA.
