# Tareas de Implementación: Consultas en lenguaje natural

> Una tarea por vez y un commit por tarea, con un mensaje que la nombre (ej. `req011 3.6: ConsultaService valida la pregunta`). Cada paquete NuGet se confirma antes de agregarlo (regla 7).

- [x] **1. Domain**
  - [x] 1.1 `ITelemetriaRepository.ObtenerAgregadoGeneralAsync(Guid? parqueSolarId, GranularidadConsulta granularidad, DateTime? desde, DateTime? hasta, CancellationToken)` → `IReadOnlyList<AgregadoPotencia>`, con su `/// <summary>`.
  - [x] 1.2 Sin tests de dominio: el requerimiento no agrega comportamiento de entidad.

- [x] **2. Infrastructure: persistencia**
  - [x] 2.1 `TelemetriaRepository`: extraer el agrupamiento día/mes/año a un método privado que recibe la consulta ya filtrada. `ObtenerAgregadoPorPanelAsync` lo usa sin cambiar su comportamiento.
  - [x] 2.2 `TelemetriaRepository.ObtenerAgregadoGeneralAsync`: filtra por parque (lectura → panel → nodo → parque) cuando `parqueSolarId` no es nulo, aplica `desde`/`hasta` y usa el método privado de 2.1.
  - [x] 2.3 Revisar en `LecturaTelemetriaConfiguration.cs` si hay índice por `fecha_hora`. Si falta, proponer una migración que solo lo agrega y **esperar aprobación** (no ejecutar `dotnet ef database update`).

- [x] **3. Application**
  - [x] 3.1 `DTOs/GeneracionPeriodoDto` (`Etiqueta`, `CantidadLecturas`, `EnergiaEstimadaKWh`).
  - [x] 3.2 `DTOs/ResumenParqueDto` (`Nombre`, `Ubicacion`, `CantidadNodos`, `CantidadPaneles`, `PanelesOperativos`, `PanelesRendimientoBajo`, `PanelesEnFalla`, `PanelesDesconectados`).
  - [x] 3.3 `DTOs/ConsultaLenguajeNaturalRequest` (`Pregunta`).
  - [x] 3.4 `DTOs/ConsultaLenguajeNaturalResponse` (`Exito`, `Texto`).
  - [x] 3.5 `Consultas/IMCPAgentService.ConsultarAsync(string pregunta, CancellationToken)`, documentando que nunca lanza por fallas del proveedor.
  - [x] 3.6 `Services/ConsultaService.ConsultarAsync`: recorta la pregunta, lanza `ReglaDeNegocioException` si está vacía o supera `LargoMaximoPregunta` (500) y delega en `IMCPAgentService`.
  - [x] 3.7 `ReporteService`: inyectar `IParqueRepository` y `TimeProvider` en el constructor.
  - [x] 3.8 `ReporteService`: extraer la fórmula de energía (kWh y redondeos) a un método privado compartido con `ObtenerGeneracionPorPanelAsync`.
  - [x] 3.9 `ReporteService.ObtenerGeneracionAsync(Guid parqueSolarId, GranularidadConsulta, DateOnly? desde, DateOnly? hasta, CancellationToken)`: parque inexistente lanza `ReglaDeNegocioException`; `hasta` incluye todo el día.
  - [x] 3.10 `ReporteService.ObtenerGeneracionHoyAsync(CancellationToken)`: desde las 00:00 de Montevideo hasta ahora (a UTC), suma los períodos devueltos.
  - [x] 3.11 `Mappings/ParqueMappings`: conteo de paneles por estado y conversión a `ResumenParqueDto`.
  - [x] 3.12 `ParqueService.ObtenerResumenAsync(Guid parqueSolarId, CancellationToken)`: parque inexistente lanza `ReglaDeNegocioException`.

- [x] **4. Paquetes, servidor MCP y registro de dependencias**

  Las clases de `Infrastructure` de esta sección (herramientas y cliente del agente) dependen de los servicios de la sección 3, por eso van después de ella.
  - [x] 4.1 Confirmar y agregar `ModelContextProtocol` a `SolarUY.Infrastructure.csproj`.
  - [x] 4.2 Confirmar y agregar `Microsoft.Extensions.AI` a `SolarUY.Infrastructure.csproj`.
  - [x] 4.3 Confirmar y agregar `Google_GenerativeAI.Microsoft` a `SolarUY.Infrastructure.csproj`.
  - [x] 4.4 `Mcp/Tools/ListarParquesTool` (sin parámetros; usa `ParqueService.ObtenerTodosAsync`).
  - [x] 4.5 `Mcp/Tools/ObtenerResumenParqueTool` (`parqueId`; usa `ParqueService.ObtenerResumenAsync`).
  - [x] 4.6 `Mcp/Tools/ObtenerGeneracionHoyTool` (sin parámetros; aclara que "hoy" es el día de Montevideo y que la energía es estimada).
  - [x] 4.7 `Mcp/Tools/ObtenerGeneracionHistoricaTool` (`parqueId`, `desde`, `hasta`, `agrupacion`; aclara que agrupa por día UTC y que la energía es estimada).
  - [x] 4.8 `Mcp/Tools/ListarPanelesEnFallaTool` (sin parámetros; usa `PanelService.ObtenerTodosAsync(EstadoSalud.FALLA_DETECTADA)`).
  - [x] 4.9 Las herramientas cuyo servicio puede lanzar `ReglaDeNegocioException` (resumen e histórica) devuelven su mensaje como texto; las demás (listar parques, generación de hoy y paneles en falla) no la capturan porque su servicio no la lanza. Ninguna llama a `Crear`, `Actualizar`, `Eliminar`, `Guardar` ni a `ISensorClient`.
  - [x] 4.10 Crear `src/SolarUY.McpServer/SolarUY.McpServer.csproj` (referencias a `Application`, `Domain` e `Infrastructure`) y agregarlo a `SolarUY.slnx`. Incluye un `Program.cs` mínimo y **provisorio** (`CreateBuilder`/`Build`/`Run`): un proyecto `Microsoft.NET.Sdk.Web` sin punto de entrada no compila, y la 4.12 lo reemplaza con la configuración definitiva.
  - [x] 4.11 Confirmar y agregar `ModelContextProtocol.AspNetCore` a `SolarUY.McpServer.csproj`.
  - [x] 4.12 `SolarUY.McpServer/Program.cs`: cadena de conexión, `AddInfrastructure`, servicios de `Application` como `Scoped`, `TimeProvider`, las 5 herramientas y el endpoint HTTP sin sesión.
  - [x] 4.13 `SolarUY.McpServer/appsettings.Development.json` con la cadena de conexión, como el del colector. Incluye `Properties/launchSettings.json` con el perfil `SolarUY.McpServer` (puerto 5099, `ASPNETCORE_ENVIRONMENT=Development`): sin ese archivo `dotnet run` arranca en `Production`, no lee `appsettings.Development.json` y no encuentra la cadena de conexión.
  - [x] 4.14 `src/SolarUY.McpServer/Dockerfile` (build en dos etapas, imagen `aspnet`, usuario no root).
  - [x] 4.15 `docker-compose.yml`: servicio `mcp` con una sola instancia, sin puertos publicados, red `solareuy`, `depends_on` MySQL saludable y healthcheck.
  - [x] 4.16 `Consultas/GeminiMcpAgentService : IMCPAgentService`: obtiene las herramientas del servidor MCP, se las manda a Gemini con la instrucción de sistema (que incluye la fecha de hoy en Montevideo, calculada desde `TimeProvider` con `ReporteService.DesfaseMontevideo`) y `Temperature = 0.2f`, ejecuta las que Gemini pida y devuelve la respuesta.
  - [x] 4.17 `GeminiMcpAgentService`: timeout configurable (30 s) y traducción de toda falla a `Exito = false` con mensaje en español.
  - [x] 4.18 `DependencyInjection.cs`: dos métodos de extensión nuevos, uno para el cliente del agente y otro para las herramientas. `AddInfrastructure` no cambia.
  - [x] 4.19 `SolarUY.Web/Program.cs`: registrar `ConsultaService` y el cliente del agente (`TimeProvider.System` ya quedó registrado en la 3.7).
  - [x] 4.20 `docker-compose.yml` (`x-web`): `Mcp__UrlServidor`, `Gemini__Modelo` y `Gemini__ApiKey: ${GEMINI_API_KEY:-}`.
  - [x] 4.21 `.env.example`: `GEMINI_API_KEY=` vacía. En desarrollo, la clave va en *user secrets*.

- [x] **5. Presentation y documentación**
  - [x] 5.1 `Models/ConsultaViewModel` (`Pregunta` con `[Required]`, `[StringLength(ConsultaService.LargoMaximoPregunta)]` y `[Display]`; `PreguntaEnviada`, `Respuesta`, `Exito`).
  - [x] 5.2 `ConsultasController.Index` GET con `[Authorize(Roles = "Administrador")]`.
  - [x] 5.3 `ConsultasController.Index` POST con `[ValidateAntiForgeryToken]`: valida `ModelState`, llama a `ConsultaService`, captura `ReglaDeNegocioException` en `ModelState` y devuelve la vista con la respuesta, sin redirigir.
  - [x] 5.4 `Views/Consultas/Index.cshtml`: campo de texto, botón **Enviar**, pregunta y respuesta, con Tag Helpers y sin JavaScript.
  - [x] 5.5 La vista muestra el aviso «No se pudo obtener una respuesta en este momento. Probá de nuevo en unos minutos.» cuando `Exito` es falso.
  - [x] 5.6 Enlace «Consultas» en `Views/Shared/_Layout.cshtml`.
  - [x] 5.7 `Documentacion/diagrama-infraestructura.md` (MCP como servicio aparte; Gemini como servicio externo) y regenerar la imagen de `entrega-1-diagramas/` si se usa en la entrega. **Documento actualizado; la imagen de la entrega (`entrega-1-diagramas/diagrama-infraestructura.png`) también incluye el servidor MCP y Gemini, así que no hace falta regenerarla.**
  - [x] 5.8 `README.md` (tabla de proyectos y checklist) y `Documentacion/DOCUMENTACION-SolarUY.md` (§5 y §11).
  - [x] 5.9 `.agents/rules/1_clean_architecture.md` (nuevo proyecto en la lista de capas) y `.agents/rules/6_flujo_trabajo_ia.md` §8 (suma `ListarParquesTool`). **Proponer el cambio y esperar aprobación**: son reglas del arnés. Se propone además la redacción de la regla 5 §4: «No capturar `Exception` genérica en controladores; si un adaptador de infraestructura convierte fallas externas en resultados, documentá la excepción y registrá el detalle técnico». **Aplicado con ajuste:** la redacción final de la regla 5 §4 reemplazó "registrá el detalle técnico" por "registra solo el tipo de excepción, nunca mensajes ni URLs que puedan contener claves", porque la clave de Gemini puede viajar en la URL.

- [x] **6. Verificación de Calidad**
  - [x] 6.1 `dotnet build` desde la raíz, sin errores ni advertencias nuevas. **Hecho: "Compilación correcta", 0 advertencias y 0 errores (8,17 s).**
  - [x] 6.2 `dotnet test tests/CodeQuality.Tests`. **Hecho el 11/11/2026:** el proyecto existe (11 reglas con ArchUnitNET) y corre con el resto de las pruebas.
  - [x] 6.3 `dotnet test tests/SolarUY.Domain.Tests`. **Hecho: 109 pasados, 0 fallados (74 ms).**
  - [x] 6.4 Analizar con SonarQube (`tools/SonarQube/analizar.sh .`; en Windows, `.\tools\SonarQube\analizar.ps1 .`) y corregir los issues nuevos *Blocker* o *Critical*. **Ejecutado el 09/11/2026. Gate Failed por causas anteriores a req011 (grupos A–E, 23 issues) y por la condición new_coverage; req011 aporta 0 issues de confiabilidad/seguridad y 7 de mantenibilidad. Se corrigió S6966 en McpServer/Program.cs. Ver SONARQUBE-SolarUY.md.**
  - [x] 6.5 Criterio 1: el Administrador ve «Consultas» en el menú y entra a la pantalla; sin sesión o sin ese rol no accede. **Verificado a mano el 09/11/2026:** sin sesión redirige al login y el Administrador ve el menú y la pantalla. No se probó un usuario sin el rol.
  - [x] 6.6 Criterio 2: dos preguntas seguidas; la segunda no usa el contexto de la primera. **Verificado a mano el 09/11/2026, respuestas correctas.**
  - [x] 6.7 Criterio 3: «¿cómo está funcionando todo hoy?» responde con parques, paneles por estado y generación de hoy (repetir con cuota de Gemini disponible; también valida el caso normal de la 4.17). **Verificado a mano el 09/11/2026:** respondió con el parque, los paneles por estado y la generación de hoy (1,4329 kWh, aclarada como estimación).
  - [x] 6.8 Criterio 3: la generación histórica de un parque por día, mes y año con rango de fechas coincide con la suma de la pantalla *Histórico* de sus paneles. **Verificado a mano el 09/11/2026:** el 18/10/2026 respondió 10,5028 kWh, igual al de la pantalla Histórico, y las agrupaciones por mes y año fueron coherentes.
  - [x] 6.9 Criterio 3: forzar una falla en el simulador y preguntar por los paneles en falla; aparece el panel y su nodo. **Verificado a mano el 09/11/2026, respuestas correctas.**
  - [x] 6.10 Criterio 4: preguntar por un parque que no existe o sin lecturas; la respuesta lo dice y no inventa valores. **Verificado a mano el 09/11/2026, respuestas correctas.**
  - [x] 6.11 Criterio 5: pedir «desactivá el panel P01» y una pregunta ajena al sistema; ambas se rechazan y el simulador no cambia. **Verificado a mano el 09/11/2026:** rechazó la orden de desactivar el panel y la pregunta ajena al sistema, y el estado de P01 no cambió.
  - [x] 6.12 Criterio 6: pregunta vacía muestra validación; con `docker compose stop mcp` o una clave inválida aparece el aviso y el resto del sistema sigue andando. **Verificado a mano el 09/11/2026:** la validación y el aviso con el MCP caído funcionaron y el resto del sistema siguió andando.
  - [x] 6.13 Criterio 7: `git grep` de la clave de Gemini no encuentra nada versionado. **Hecho: ningún valor versionado; solo nombres de variables vacías y dos falsos positivos en `jquery*.min.map`.**
  - [x] 6.14 El servidor MCP no es alcanzable desde el host ni a través de nginx (sin puertos publicados). **Verificado el 09/11/2026:** el MCP no tiene puerto publicado (`docker compose port mcp 8080` devuelve `:0` y `docker inspect` muestra `{"8080/tcp":null}`); el puerto 8080 del host es nginx y `/mcp` por nginx devuelve 404; solo `web1` alcanza `http://mcp:8080/health` por la red interna.
  - [x] 6.15 Completar la sección `## Uso de IA` (ver regla `6_flujo_trabajo_ia.md`, sección 9).

## Uso de IA

- **Proceso:** arnés SDD (requirements → specs → tasks → código), **una tarea por vez**, con aprobación humana
  de cada cambio a partir de su **diff real**, y **un commit por tarea**.
- **Revisión externa:** un segundo asistente (Claude) se usó fuera del repositorio para redactar los prompts de
  cada tarea y revisar los diffs; propuso ajustes puntuales que la persona aplicó a mano; no escribió en el
  repo.
- **Código, `specs.md` y `tasks.md`:** generados con asistencia de IA y revisados por Mariano tarea por tarea.
- **Herramienta que generó el código:** *Claude*

### Decisiones y su razón

- **Servidor MCP en un proceso aparte** (`SolarUY.McpServer`), alcanzable **solo desde la red interna y sin
  autenticación**, porque no se publica.
- **La clave de Gemini vive solo en la web**; el servidor MCP conoce únicamente la base.
- **Herramientas de solo lectura por construcción:** ninguna llama a métodos de escritura, verificado con
  `git grep`.
- **Transporte HTTP sin estado:** cada consulta abre su propio cliente MCP y no hace falta mantener sesiones.
- **Tope de 8 vueltas de herramientas y timeout de 30 s** por consulta.
- **Las fallas se convierten en un mensaje genérico** y se registra **solo el tipo de excepción**, porque la
  clave puede viajar en la URL.
- **«Hoy» es el día calendario de Montevideo** con desfase fijo UTC−3; el histórico agrupa por **día UTC**.

### Errores detectados al revisar o al ejecutar

- **`WithTools` con la sobrecarga equivocada:** el servidor arrancaba **sin herramientas** porque un `Type[]`
  caía en la sobrecarga de «instancia target». Se corrigió declarando el argumento como `IEnumerable<Type>`.
- **Enum serializado como número:** las herramientas devolvían el estado del panel como número y el modelo no
  podía interpretarlo; se corrigió agregando `JsonStringEnumConverter` a `OpcionesJsonMcp`, de modo que sale
  como texto.
- **Bug del controlador:** `ModelState.Remove` borraba el error y vaciaba el texto **también en las fallas**. Se
  corrigió para que el vaciado ocurra solo cuando la consulta fue exitosa.
- **`Trim()` sobre un string nulo:** en `ObtenerGeneracionHistoricaTool`, `agrupacion.Trim()` fallaba si el
  parámetro llegaba nulo; fue un riesgo detectado en la revisión y se corrigió con `string?` y
  `agrupacion?.Trim().ToLowerInvariant()`.
- **Faltaba `launchSettings.json`:** `dotnet run` arrancaba en `Production` y no leía el archivo de Development.
- **El primer prompt de la 4.16 omitía** la fecha, la prohibición de Markdown y el rechazo de preguntas ajenas;
  se amplió la instrucción de sistema.
- **Versión de paquete supuesta:** se creía que `Google_GenerativeAI.Microsoft` era 3.6.7 y era 3.6.8; se
  verificó contra el índice de NuGet antes de instalar.
- **Modelo `gemini-2.5-flash` discontinuado (404)** y **cuota gratuita de unos 20 pedidos diarios (429)**, más
  **503 transitorios**: se cambió de modelo y quedó anotada la limitación.
- **La clave apareció en el texto de una conversación con un agente:** se recomendó rotarla.
- **Un comando de PowerShell sugerido por el asistente de revisión externo dañó la codificación de `tasks.md`**,
  detectado por la verificación del agente y corregido.

### Verificación

- `dotnet build`: **sin advertencias**.
- `dotnet test tests/SolarUY.Domain.Tests`: **109 tests** en verde.
- **SonarQube:** 0 issues nuevos de confiabilidad o seguridad de req011, y 7 issues nuevos de mantenibilidad
  (Low/Info), documentados en `SONARQUBE-SolarUY.md`; el *gate* queda *Failed* por causas previas y por la
  cobertura.
- **El servidor MCP no es alcanzable** desde el host ni a través de nginx.
- **Criterios 1 a 6 probados a mano** (del criterio 1 no se probó un usuario sin el rol de Administrador); el
  criterio 7 se verificó con `git grep`.
- **Tests de arquitectura:** ejecutados el 11/11/2026, cuando `tests/CodeQuality.Tests` (ArchUnitNET) se sumó al
  repositorio: 11 reglas en verde (ver 6.2).

### Límites y mejoras

- Usuario de base de datos de **solo lectura** para el MCP.
- **Regla de ArchUnit contra escrituras desde `Mcp`**.
- Pruebas de `ReporteService` con `FakeTimeProvider`.
- **Cobertura en el análisis** de SonarQube.
- `PanelService.ObtenerTodosAsync` **filtra en memoria**.
- `AddInfrastructure` **registra `ISensorClient` también en el `McpServer`**.
- **Commits agrupados** (`48eecb4`, `7e34394`).
