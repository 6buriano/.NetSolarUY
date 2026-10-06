# Clean Architecture - SolarUY

## Capas de la Solución (.NET Solution)

1. **`SolarUY.Domain` (Core - Sin dependencias)**
   - Contiene Entidades (`ParqueSolar`, `Nodo`, `Inversor`, `PanelSolar`, `TareaMantenimiento`).
   - Value Objects, Enums (`EstadoSalud`, `TipoMantenimiento`) e interfaces de Repositorios.
   - Excepciones de Dominio.

2. **`SolarUY.Application` (Casos de Uso)**
   - Servicios de Aplicación (DTOs, Mappings).
   - Interfaces para servicios externos (Servicio de Notificación SignalR, Cliente MCP, Seguridad).
   - Validaciones de negocio (FluentValidation).

3. **`SolarUY.Infrastructure` (Detalles de Implementación)**
   - `SolarUYDbContext` (Entity Framework Core con MySQL).
   - Implementación de Repositorios.
   - Integración con Keycloak (OIDC Client).
   - Cliente/Servidor MCP para Gemini.

4. **`SolarUY.Web` (Capa de Presentación)**
   - Controladores MVC y Vistas Razor (`.cshtml`).
   - Hubs de SignalR para métricas en vivo.
   - Middleware de manejo de errores y autenticación.

5. **`SolarUY.Simulator` (Worker Service)**
   - Proceso independiente que emula lecturas del Sensor ARM y registra en MySQL/SignalR.

## Reglas de Dependencia
- `Domain` no conoce a ninguna otra capa.
- `Application` solo conoce a `Domain`.
- `Infrastructure` y `Web` dependen de `Application` y `Domain`.
- La inversión de dependencias (IoC Container de ASP.NET Core) se configura en `SolarUY.Web`.
