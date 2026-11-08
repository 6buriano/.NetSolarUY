# Especificaciones del Requerimiento 011: Consultas en lenguaje natural

## Resumen

Módulo `Consultas`. La pantalla **Consultas** recibe la pregunta, la web consulta a **Gemini** y este usa 5 herramientas de solo lectura que corren en un **proceso aparte** (`SolarUY.McpServer`) y reutilizan servicios de `Application`. La clave de Gemini solo la conoce la web; el servidor MCP solo conoce la base. Sin cambios de esquema.

**Decisiones tomadas**
- **Servidor MCP:** proyecto `SolarUY.McpServer`, servicio `mcp`, puerto interno 8080, **una instancia**. Sin autenticación: no publica puerto ni pasa por nginx, solo lo alcanzan los contenedores de la red de Docker (como el colector).
- **`parqueId`:** Gemini no puede adivinar un Guid, así que se agrega una quinta herramienta, `ListarParquesTool`. Las firmas de la regla 8 no cambian; la regla se actualiza solo para sumar esa herramienta.
- **Panel "en falla":** solo `FALLA_DETECTADA`. Los demás estados se ven en el conteo del resumen.
- **"Hoy":** día calendario de Montevideo (UTC-3), de las 00:00 hasta ahora. El histórico sigue agrupando por día UTC; la descripción de la herramienta lo aclara.
- **Gemini con paquetes NuGet** (confirmación de cada uno requerida por la regla 7; las versiones se verifican al agregarlos): en `Infrastructure`, `ModelContextProtocol`, `Microsoft.Extensions.AI` y `Google_GenerativeAI.Microsoft`; en `SolarUY.McpServer`, la extensión de MCP para ASP.NET Core (`ModelContextProtocol.AspNetCore`). El ciclo de herramientas lo resuelve la librería; para la defensa, el flujo está explicado en "Cliente del agente".

## 1. Capa Domain

Sin entidades ni lógica nueva, así que no hay pruebas de dominio. Un método nuevo en `ITelemetriaRepository`:

- `ObtenerAgregadoGeneralAsync(Guid? parqueSolarId, GranularidadConsulta granularidad, DateTime? desde, DateTime? hasta, CancellationToken)` → `IReadOnlyList<AgregadoPotencia>`: como `ObtenerAgregadoPorPanelAsync`, pero sobre todos los paneles (Id nulo) o los de un parque.

## 2. Capa Application

**DTOs**
- `ConsultaLenguajeNaturalRequest(string Pregunta)`
- `ConsultaLenguajeNaturalResponse(bool Exito, string Texto)`
- `ResumenParqueDto(Nombre, Ubicacion, CantidadNodos, CantidadPaneles, PanelesOperativos, PanelesRendimientoBajo, PanelesEnFalla, PanelesDesconectados)`
- `GeneracionPeriodoDto(Etiqueta, CantidadLecturas, EnergiaEstimadaKWh)`. No se reutiliza `ResumenGeneracionDto` porque su potencia promedio y máxima son de un panel y confundirían al modelo.

**`Consultas/IMCPAgentService`**: `ConsultarAsync(string pregunta, CancellationToken)` → `ConsultaLenguajeNaturalResponse`. Nunca lanza por fallas del proveedor: devuelve `Exito = false` con mensaje en español.

**`ConsultaService`** (nuevo): `ConsultarAsync(ConsultaLenguajeNaturalRequest, CancellationToken)`. Recorta la pregunta; si está vacía o supera `LargoMaximoPregunta` (500), lanza `ReglaDeNegocioException`; si no, delega en `IMCPAgentService`.

**`ReporteService`** (existente, se le inyecta `IParqueRepository`)
- `ObtenerGeneracionAsync(Guid parqueSolarId, GranularidadConsulta granularidad, DateOnly? desde, DateOnly? hasta, CancellationToken)` → `IReadOnlyList<GeneracionPeriodoDto>`. Parque inexistente: `ReglaDeNegocioException`. `hasta` incluye todo el día (como el req010). La fórmula de energía se extrae a un método privado compartido con `ObtenerGeneracionPorPanelAsync`.
- `ObtenerGeneracionHoyAsync(CancellationToken)` → `GeneracionPeriodoDto` de **todos los parques**: desde las 00:00 de Montevideo hasta ahora (convertido a UTC), sumando los períodos devueltos. Usa `TimeProvider` inyectado.

**`ParqueService`** (existente): `ObtenerResumenAsync(Guid parqueSolarId, CancellationToken)` → `ResumenParqueDto`. El conteo por estado va en `ParqueMappings`. Parque inexistente: `ReglaDeNegocioException`. Para listar parques se reutiliza `ObtenerTodosAsync` sin cambios.

**`PanelService`**: sin cambios; se usa `ObtenerTodosAsync(EstadoSalud.FALLA_DETECTADA)`.

## 3. Capa Infrastructure

**Persistencia**
- `TelemetriaRepository.ObtenerAgregadoGeneralAsync`: filtra por parque (lectura → panel → nodo → parque) y **agrega en la base**. El agrupamiento día/mes/año se extrae a un método privado compartido con `ObtenerAgregadoPorPanelAsync` para no duplicar el `switch`; el comportamiento del existente no cambia.
- Sin migración. Se revisa si hay índice por `fecha_hora`; si falta, se propone otra migración que solo lo agrega (compatible, no se aplica sin pedido).

**Herramientas** (`Mcp/Tools/`, sufijo `Tool`, nombre y descripción en español)

| Clase | Parámetros | Usa |
|---|---|---|
| `ListarParquesTool` | ninguno | `ParqueService.ObtenerTodosAsync` |
| `ObtenerResumenParqueTool` | `parqueId` | `ParqueService.ObtenerResumenAsync` |
| `ObtenerGeneracionHoyTool` | ninguno | `ReporteService.ObtenerGeneracionHoyAsync` |
| `ObtenerGeneracionHistoricaTool` | `parqueId`, `desde`, `hasta`, `agrupacion` | `ReporteService.ObtenerGeneracionAsync` |
| `ListarPanelesEnFallaTool` | ninguno | `PanelService.ObtenerTodosAsync` |

Solo lectura por construcción: ninguna llama a `Crear`, `Actualizar` ni `Eliminar`, ni usa `ISensorClient`. Si recibe una `ReglaDeNegocioException`, devuelve su mensaje como texto. Las de energía aclaran que es una estimación y la de "hoy" que usa el día de Montevideo.

**Cliente del agente** (`Consultas/GeminiMcpAgentService : IMCPAgentService`): el flujo de la regla 8 §8: obtiene las herramientas del servidor MCP, se las manda a Gemini con la pregunta, ejecuta las que Gemini pida y repite hasta tener la respuesta. Instrucción de sistema: responder en español, solo con datos de las herramientas, sin inventar, usar `ListarParquesTool` para conocer el `parqueId` y rechazar pedidos que cambien datos. Timeout configurable (30 s). Cualquier falla se convierte en `Exito = false`. Sin estado entre consultas.

**Registro:** dos extensiones nuevas en `DependencyInjection` (cliente para la web, herramientas para el servidor MCP). `AddInfrastructure` no cambia.

**`src/SolarUY.McpServer`** (proyecto web nuevo, tercer punto de arranque): referencia `Application`, `Domain` e `Infrastructure` (esta solo en su `Program.cs`) y expone las herramientas por HTTP sin sesión. Dockerfile como el del colector pero con la imagen `aspnet` y usuario no root; servicio `mcp` en el compose sin puertos publicados y con healthcheck; se agrega a `SolarUY.slnx`.

## 4. Capa Presentation (MVC)

- **`ConsultasController`**, `[Authorize(Roles = "Administrador")]`:
  - `[HttpGet] Index`: formulario vacío.
  - `[HttpPost] [ValidateAntiForgeryToken] Index(ConsultaViewModel)`: valida `ModelState`, llama a `ConsultaService`, captura `ReglaDeNegocioException` en `ModelState` y devuelve la vista con la respuesta. **No redirige**: la acción no modifica datos y redirigir obligaría a guardar la respuesta en `TempData`.
- **`ConsultaViewModel`**: `Pregunta` con `[Required]` y `[StringLength(ConsultaService.LargoMaximoPregunta)]`; `PreguntaEnviada`, `Respuesta`, `Exito`.
- **Vista** `Views/Consultas/Index.cshtml`: campo, botón **Enviar**, respuesta debajo y aviso si `Exito` es falso. Sin JavaScript. Enlace «Consultas» en el menú.
- **`Program.cs`** registra `ConsultaService` y el cliente. Configuración: `Mcp:UrlServidor`, `Gemini:ApiKey` (secreto), `Gemini:Modelo`.
- **Secretos:** *user secrets* en desarrollo y `GEMINI_API_KEY` en el servidor (en `x-web` del compose y vacía en `.env.example`).
- **Mensaje ante falla:** «No se pudo obtener una respuesta en este momento. Probá de nuevo en unos minutos.»

**Documentación que cambia:** diagrama de infraestructura, `README.md`, `DOCUMENTACION-SolarUY.md` (§5 y §11) y dos reglas del arnés: `1_clean_architecture.md` (nuevo proyecto) y `6_flujo_trabajo_ia.md` §8 (suma `ListarParquesTool`). Las reglas las aprueba una persona antes de aplicar el cambio.

## Trazabilidad

| Criterio | Elemento del diseño |
|---|---|
| 1. Pantalla, menú, rol | `ConsultasController`, `Views/Consultas/Index.cshtml`, `[Authorize(Roles = "Administrador")]` |
| 2. Una pregunta, una respuesta | POST sin sesión ni historial; una conexión por consulta |
| 3. Las 4 consultas | `ObtenerResumenParqueTool`, `ObtenerGeneracionHoyTool`, `ObtenerGeneracionHistoricaTool`, `ListarPanelesEnFallaTool` (+ `ListarParquesTool` para obtener el `parqueId`); «estimación» en la descripción de las de energía |
| 4. Datos reales | Instrucción de sistema + herramientas; parque inexistente o sin lecturas se informa por texto |
| 5. Solo lectura y temas ajenos | Herramientas solo de consulta + instrucción de sistema |
| 6. Vacía y falla del servicio | `ConsultaService`, `[Required]`, `Exito = false` |
| 7. Clave fuera del repo | `Gemini:ApiKey`, `GEMINI_API_KEY`; solo la web |
