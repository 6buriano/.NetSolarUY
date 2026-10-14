---
trigger: always_on
---

# Clean Architecture - SolarUY

SolarUY es una prueba de concepto para gestionar parques solares: monolito modular guiado por Clean Architecture, con ASP.NET Core MVC, EF Core sobre MySQL, Keycloak (OIDC), SignalR con Redis backplane y un servidor MCP conectado a Gemini. Al generar, refactorizar o revisar código, el agente debe respetar estas reglas. Varias se validan automáticamente con los tests de ArchUnitNET del proyecto `CodeQuality.Tests`.

## Capas de la Solución (.NET Solution)

1. `SolarUY.Domain` (Core - Sin dependencias)
   * Contiene Entidades (`ParqueSolar`, `Nodo`, `Inversor`, `PanelSolar`, `SensorDispositivo`, `LecturaTelemetria`, `TareaMantenimiento`).
   * Value Objects, Enums (`EstadoSalud`, `TipoMantenimiento`, `EstadoMantenimiento`) e interfaces de Repositorios.
   * Excepciones de Dominio.
2. `SolarUY.Application` (Casos de Uso)
   * Servicios de Aplicación, DTOs y Mappings.
   * Interfaces para servicios externos (Servicio de Notificación SignalR, Cliente MCP, Seguridad).
   * Validaciones de negocio (FluentValidation).
3. `SolarUY.Infrastructure` (Detalles de Implementación)
   * `SolarUYDbContext` (Entity Framework Core con MySQL, proveedor Pomelo).
   * Configuraciones de EF (`IEntityTypeConfiguration<T>`) y migraciones.
   * Implementación de Repositorios.
   * Integración con Keycloak (OIDC Client).
   * Cliente/Servidor MCP para Gemini.
4. `SolarUY.Web` (Capa de Presentación)
   * Controladores MVC, ViewModels y Vistas Razor (`.cshtml`).
   * Hubs de SignalR para métricas en vivo.
   * Middleware de manejo de errores y autenticación.
5. `SolarUY.Collector` (Worker Service)
   * Proceso independiente que cada pocos segundos consulta a los sensores (por `ISensorClient`), deja que el dominio diagnostique las lecturas y las guarda en MySQL.
6. `SolarUY.SensorSimulator` (submódulo, repositorio aparte)
   * Mock del hardware: emula los sensores ARM e inversores por HTTP. No es parte de la solución de negocio y no se modifica desde este repo.

## Reglas de Dependencia

* `Domain` no conoce a ninguna otra capa. **NUNCA** importa paquetes de Entity Framework Core, ASP.NET, SignalR ni nada externo: solo C#.
* `Application` solo conoce a `Domain`. No conoce controladores, `IActionResult`, `ModelState`, ViewModels, `DbContext` ni SQL.
* `Infrastructure` depende de `Application` y `Domain`, e implementa sus interfaces.
* `Web` depende de `Application` y `Domain`. **Los controladores NO usan el `DbContext` ni los repositorios concretos**: llaman siempre a un servicio de `Application` a través de su interfaz.
   * La inversión de dependencias (IoC Container de ASP.NET Core) se configura en `Program.cs` de `SolarUY.Web`. Es la **única** referencia permitida de `Web` hacia `Infrastructure`, y se limita a llamar un método de registro (ej. `builder.Services.AddInfrastructure(builder.Configuration)`).
* `Collector` es otro punto de arranque: puede referenciar `Infrastructure` solo en su `Program.cs`, igual que `Web`. Su lógica usa interfaces de `Application`.
* Ningún proyecto de `src/` referencia al `SensorSimulator`: se le habla solo por HTTP, como a un hardware real.

> ⚠️ El scaffolding de Visual Studio ("Controlador de MVC con vistas, usando Entity Framework") genera controladores que inyectan el `DbContext`. Ese código **rompe** la regla de dependencia. Puede usarse para obtener las vistas, pero el controlador se reescribe para usar el servicio.

## Módulos (Monolito Modular)

> ⚠️ Hoy el código está organizado por tipo (`Entities/`, `Services/`, `DTOs/`, `Controllers/`…). Separarlo en carpetas por módulo es una decisión pendiente del equipo: hasta que se tome, el agente **no** reorganiza carpetas y sigue la estructura existente. Los módulos de negocio son:

| Módulo | Contenido |
|---|---|
| `Gestion` | Parques, nodos, inversores y paneles |
| `Monitoreo` | Telemetría, métricas en tiempo real e histórico |
| `Mantenimiento` | Tareas de mantenimiento |
| `Consultas` | Chat en lenguaje natural y herramientas MCP |

* Un módulo **no** usa los repositorios de otro módulo. Si `Mantenimiento` necesita un panel, se lo pide al servicio de `Gestion`.
* Si una clase empieza a ser usada por varios módulos, el agente lo advierte en lugar de moverla por su cuenta.

## Repositorios e Inyección de Dependencias

* Las interfaces de repositorio (`IParqueRepository`) van en `Domain`; su implementación (`ParqueRepository`) va **obligatoriamente** en `Infrastructure`.
* Las dependencias se inyectan **exclusivamente por constructor**. No se instancian servicios ni repositorios con `new`.
* Servicios y repositorios se registran como `Scoped`. Nada que dependa del `DbContext` se registra como `Singleton`.

## Reglas por el Despliegue (Alta Disponibilidad)

La aplicación corre en varias réplicas idénticas detrás de un balanceador (round robin + sticky sessions) y se actualiza con rolling update. Por eso:

* **Sin estado en memoria.** No usar variables `static`, singletons con datos de usuario ni `Session` en memoria para guardar datos entre pedidos. Todo lo que deba persistir va a MySQL; los eventos entre réplicas viajan por Redis.
* **Claves compartidas.** Las claves de Data Protection (firman las cookies de login y los tokens antiforgery) se guardan en un lugar común a todas las réplicas (ej. Redis). Si se toca la configuración de autenticación o cookies, advertirlo.
* **Migraciones compatibles.** Durante una actualización conviven la versión vieja y la nueva contra la misma base. Una migración no puede borrar ni renombrar una columna que la versión anterior usa. Si una migración lo hace, advertirlo antes de generarla.
* **Un solo colector.** El `Collector` corre en una sola instancia; dos instancias duplicarían las lecturas.
