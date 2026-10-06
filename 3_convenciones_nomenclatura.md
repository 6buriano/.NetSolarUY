# Estándares y Convenciones de Código

## C# (.NET)
- **PascalCase:** Clases, Métodos, Propiedades, Enums, Namespaces (`ParqueSolar`, `CalcularGeneracionTotal()`).
- **camelCase:** Parámetros de métodos y variables locales (`parqueId`, `corrienteGenerada`).
- **_camelCase:** Campos privados de clase (`_dbContext`, `_logger`).
- **I + PascalCase:** Interfaces (`IParqueRepository`).

## Base de Datos (MySQL)
- **Tables:** Plural en Snake_case o PascalCase (`parques_solares`, `lecturas_telemetria`).
- **Primary Keys:** `id` (Guid / UUID).
- **Foreign Keys:** `parque_id`, `nodo_id`.

## Archivos y Frontend
- **Vistas Razor:** PascalCase (`Index.cshtml`, `DetalleParque.cshtml`).
- **Controladores:** `NombreControlador.cs` (`ParquesController.cs`).
- **JavaScript / SignalR:** camelCase (`signalrHub.js`, `updateTelemetryData()`).
