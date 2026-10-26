---
trigger: always_on
---

# Clean Architecture - SolarUY

SolarUY es una prueba de concepto para gestionar parques solares: monolito modular guiado por Clean Architecture, con ASP.NET Core MVC, EF Core sobre MySQL, Keycloak (OpenID Connect) y, en próximas iteraciones, SignalR y un servidor MCP conectado a Gemini. Al generar, refactorizar o revisar código, el agente debe respetar estas reglas. Varias se validan automáticamente con los tests de ArchUnitNET del proyecto `tests/CodeQuality.Tests`.

## Capas de la Solución (.NET Solution)

1. `SolarUY.Domain` (Core - Sin dependencias)
   * Entidades (`ParqueSolar`, `Nodo`, `Inversor`, `PanelSolar`, `SensorDispositivo`, `LecturaTelemetria`, `TareaMantenimiento`).
   * Enums (`EstadoSalud`, `TipoMantenimiento`, `EstadoMantenimiento`) e interfaces de repositorios (`Repositories/`).
   * Excepciones de dominio (`ReglaDeNegocioException`).
2. `SolarUY.Application` (Casos de Uso)
   * Servicios de aplicación (`Services/`), DTOs (`DTOs/`) y mapeos (`Mappings/`).
   * Interfaces de servicios externos (`Sensors/ISensorClient`, `ISimuladorAdminClient`, `ISensorDisponibilidadService`). Más adelante: notificación en tiempo real y cliente MCP.
3. `SolarUY.Infrastructure` (Detalles de Implementación)
   * `SolarUYDbContext` (EF Core con MySQL, proveedor Pomelo), configuraciones (`IEntityTypeConfiguration<T>`) y migraciones.
   * Implementación de repositorios.
   * Clientes HTTP del sensor y del simulador (`Sensors/`).
   * Registro de todo lo anterior en `DependencyInjection.AddInfrastructure(...)`.
4. `SolarUY.Web` (Capa de Presentación)
   * Controladores MVC, ViewModels y vistas Razor (`.cshtml`).
   * Autenticación con Keycloak (cookie + OpenID Connect), configurada en `Program.cs`.
   * Más adelante: hubs de SignalR para las métricas en vivo.
5. `SolarUY.Collector` (Worker Service)
   * Proceso independiente que cada pocos segundos consulta a los sensores (por `ISensorClient`), deja que el dominio diagnostique las lecturas y las guarda en MySQL.
6. `SolarUY.SensorSimulator` (submódulo, repositorio aparte)
   * Mock del hardware: emula los sensores ARM e inversores por HTTP. No es parte de la solución de negocio y no se modifica desde este repo.

## Reglas de Dependencia

* `Domain` no conoce a ninguna otra capa. **NUNCA** importa paquetes de Entity Framework Core, ASP.NET ni nada externo: solo C#.
* `Application` solo conoce a `Domain`. No conoce controladores, `IActionResult`, `ModelState`, ViewModels, `DbContext` ni SQL.
* `Infrastructure` depende de `Application` y `Domain`, e implementa sus interfaces.
* `Web` depende de `Application` y `Domain`. **Los controladores NO usan el `DbContext` ni los repositorios concretos**: llaman siempre a un servicio de `Application`.
   * La inversión de dependencias (contenedor de ASP.NET Core) se configura en `Program.cs` de `SolarUY.Web`. Es la **única** referencia permitida de `Web` hacia `Infrastructure` (`builder.Services.AddInfrastructure(connectionString)` y la elección de implementaciones según el entorno).
* `Collector` es otro punto de arranque: puede referenciar `Infrastructure` solo en su `Program.cs`, igual que `Web`. Su lógica usa interfaces de `Application`.
* Ningún proyecto de `src/` referencia al `SensorSimulator`: se le habla solo por HTTP, como a un hardware real.
* La identidad del usuario (Keycloak, roles) es responsabilidad de `Web`. `Domain` y `Application` no reciben ni conocen al usuario.

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

La web corre en **dos réplicas idénticas** (`web1` y `web2`) detrás de **nginx en modo activo/pasivo**: `web1` atiende todo y `web2` es la reserva (`backup`) a la que nginx pasa el tráfico si `web1` no responde. La actualización se hace réplica por réplica (`Documentacion/actualizacion-sin-cortes.md`). Por eso:

* **Sin estado en memoria.** No usar variables `static`, singletons con datos de usuario ni `Session` en memoria para guardar datos entre pedidos: se perderían al cambiar de réplica. Todo lo que deba persistir va a MySQL.
* **Claves compartidas.** Las claves de Data Protection (cifran la cookie de sesión de Keycloak, los tokens antiforgery y el `TempData`) viven en un volumen común a las dos réplicas (`DataProtection:Directory`). Si se toca la configuración de autenticación, cookies o Data Protection, advertirlo.
* **Detrás de un proxy.** La web se entera de su dirección real por las cabeceras `X-Forwarded-*` de nginx (`UseForwardedHeaders`). No armar URLs a mano con `Request.Host` sin tenerlo en cuenta.
* **Migraciones compatibles.** Durante una actualización conviven la versión vieja y la nueva contra la misma base. Una migración no puede borrar ni renombrar una columna que la versión anterior usa. Si una migración lo hace, advertirlo antes de generarla. Las migraciones las aplica el paso `migracion` (`--migrate-only`), no las réplicas al arrancar.
* **Un solo colector.** El `Collector` corre en una sola instancia; dos instancias duplicarían las lecturas.
* Si en el futuro se pasa a balanceo con las dos réplicas activas, SignalR va a necesitar un backplane (Redis) y sticky sessions. Mientras sea activo/pasivo, no hacen falta.
