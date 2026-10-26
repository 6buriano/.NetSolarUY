---
trigger: always_on
---

# Estándares y Convenciones de Código

El agente respeta estrictamente estas convenciones al generar o refactorizar código. Algunas se validan con ArchUnitNET.

## C# (.NET)

* **PascalCase:** Clases, Métodos, Propiedades, Enums, Namespaces (`ParqueSolar`, `CalcularGeneracionTotalActual()`).
* **camelCase:** Parámetros de métodos y variables locales (`parqueId`, `corrienteGenerada`).
* **_camelCase:** Campos privados de clase, en especial los inyectados por constructor (`_parqueRepository`, `_logger`).
* **I + PascalCase:** Interfaces (`IParqueRepository`).
* **Métodos asíncronos:** terminan en `Async` (`CrearAsync`, `ObtenerPorIdAsync`). *Excepción:* las acciones de los controladores.
* **Valores de enum:** en MAYÚSCULAS con guion bajo, como en el diagrama de clases (`FALLA_DETECTADA`, `EN_PROCESO`).

## Sufijos por tipo de clase

| Tipo | Sufijo | Capa | Ejemplo |
|---|---|---|---|
| Repositorio | `Repository` | Infrastructure (interfaz en Domain) | `PanelRepository` |
| Servicio / caso de uso | `Service` | Application | `MonitoreoService` |
| Objeto de transferencia | `Dto` | Application | `ParqueDto` |
| Pedido de entrada a un servicio | `Request` | Application | `CrearParqueRequest` |
| Respuesta de un servicio | `Response` | Application | `ConsultaLenguajeNaturalResponse` |
| Excepción | `Exception` | Domain | `ReglaDeNegocioException` |
| Controlador | `Controller` (en plural) | Web | `ParquesController` |
| Modelo de una vista | `ViewModel` | Web | `ParqueFormViewModel` |
| Hub de SignalR | `Hub` | Web | `TelemetryHub` |
| Herramienta MCP | `Tool` | Infrastructure | `ObtenerGeneracionHoyTool` |

No se usan "Dao", "Manager", "Helper" ni "GestorDatos". No se usa CQRS ni MediatR.

## Idioma

* **Dominio en español:** entidades, propiedades, métodos de negocio y variables (`TareaMantenimiento`, `CompletarTarea()`).
* **Sufijos técnicos en inglés** y sin traducir: `NodoRepository`, no `RepositorioNodo`.
* **Sin tildes ni ñ en identificadores:** `Ubicacion`, no `Ubicación`. Las tildes van solo en los textos de pantalla.
* **Unidades en el nombre:** toda magnitud física indica su unidad, tal como en el diagrama (`CapacidadInstaladaKW`, `PotenciaNominalW`, `TemperaturaCelsius`).

## Base de Datos (MySQL)

* **Tablas:** plural en `snake_case` (`parques_solares`, `lecturas_telemetria`).
* **Columnas:** `snake_case` (`capacidad_instalada_kw`).
* **Primary Keys:** `id` (Guid / UUID).
* **Foreign Keys:** `<entidad>_id` (`parque_solar_id`, `nodo_id`).
* Las propiedades de C# mantienen PascalCase (`ParqueSolarId`); la conversión a `snake_case` se hace en las configuraciones de EF de `Infrastructure`, nunca renombrando las propiedades del dominio.

## Archivos y Frontend

* **Controladores:** `NombreEnPluralController.cs`, uno por recurso (`ParquesController.cs`, `NodosController.cs`, `PanelesController.cs`).
* **Acciones y vistas del CRUD:** `Index`, `Detalle`, `Crear`, `Editar`, `Eliminar` (`Views/Parques/Crear.cshtml`). Usar siempre estos nombres, en todos los controladores.
* **Vistas parciales:** empiezan con guion bajo (`Views/Parques/_Formulario.cshtml`).
* **JavaScript / SignalR:** camelCase (`telemetriaHub.js`, `actualizarGrafico()`).
