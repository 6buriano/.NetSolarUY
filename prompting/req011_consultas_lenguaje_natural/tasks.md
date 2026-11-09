# Tareas de Implementación: Consultas en lenguaje natural

> Una tarea por vez y un commit por tarea, con un mensaje que la nombre (ej. `req011 3.6: ConsultaService valida la pregunta`). Cada paquete NuGet se confirma antes de agregarlo (regla 7).

- [ ] **1. Domain**
  - [x] 1.1 `ITelemetriaRepository.ObtenerAgregadoGeneralAsync(Guid? parqueSolarId, GranularidadConsulta granularidad, DateTime? desde, DateTime? hasta, CancellationToken)` → `IReadOnlyList<AgregadoPotencia>`, con su `/// <summary>`.
  - [ ] 1.2 Sin tests de dominio: el requerimiento no agrega comportamiento de entidad.

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
  - [ ] 3.10 `ReporteService.ObtenerGeneracionHoyAsync(CancellationToken)`: desde las 00:00 de Montevideo hasta ahora (a UTC), suma los períodos devueltos.
  - [x] 3.11 `Mappings/ParqueMappings`: conteo de paneles por estado y conversión a `ResumenParqueDto`.
  - [x] 3.12 `ParqueService.ObtenerResumenAsync(Guid parqueSolarId, CancellationToken)`: parque inexistente lanza `ReglaDeNegocioException`.

- [ ] **4. Paquetes, servidor MCP y registro de dependencias**

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

- [ ] **5. Presentation y documentación**
  - [x] 5.1 `Models/ConsultaViewModel` (`Pregunta` con `[Required]`, `[StringLength(ConsultaService.LargoMaximoPregunta)]` y `[Display]`; `PreguntaEnviada`, `Respuesta`, `Exito`).
  - [x] 5.2 `ConsultasController.Index` GET con `[Authorize(Roles = "Administrador")]`.
  - [x] 5.3 `ConsultasController.Index` POST con `[ValidateAntiForgeryToken]`: valida `ModelState`, llama a `ConsultaService`, captura `ReglaDeNegocioException` en `ModelState` y devuelve la vista con la respuesta, sin redirigir.
  - [x] 5.4 `Views/Consultas/Index.cshtml`: campo de texto, botón **Enviar**, pregunta y respuesta, con Tag Helpers y sin JavaScript.
  - [x] 5.5 La vista muestra el aviso «No se pudo obtener una respuesta en este momento. Probá de nuevo en unos minutos.» cuando `Exito` es falso.
  - [x] 5.6 Enlace «Consultas» en `Views/Shared/_Layout.cshtml`.
  - [x] 5.7 `Documentacion/diagrama-infraestructura.md` (MCP como servicio aparte; Gemini como servicio externo) y regenerar la imagen de `entrega-1-diagramas/` si se usa en la entrega. **Documento actualizado; la imagen no se regeneró por pedido explícito: queda desactualizada y hay que regenerarla antes de la entrega.**
  - [x] 5.8 `README.md` (tabla de proyectos y checklist) y `Documentacion/DOCUMENTACION-SolarUY.md` (§5 y §11).
  - [x] 5.9 `.agents/rules/1_clean_architecture.md` (nuevo proyecto en la lista de capas) y `.agents/rules/6_flujo_trabajo_ia.md` §8 (suma `ListarParquesTool`). **Proponer el cambio y esperar aprobación**: son reglas del arnés. Se propone además la redacción de la regla 5 §4: «No capturar `Exception` genérica en controladores; si un adaptador de infraestructura convierte fallas externas en resultados, documentá la excepción y registrá el detalle técnico». **Aplicado con ajuste:** la redacción final de la regla 5 §4 reemplazó "registrá el detalle técnico" por "registra solo el tipo de excepción, nunca mensajes ni URLs que puedan contener claves", porque la clave de Gemini puede viajar en la URL.

- [ ] **6. Verificación de Calidad**
  - [x] 6.1 `dotnet build` desde la raíz, sin errores ni advertencias nuevas. **Hecho: "Compilación correcta", 0 advertencias y 0 errores (8,17 s).**
  - [ ] 6.2 `dotnet test tests/CodeQuality.Tests` (el proyecto todavía no existe en el repo: hay que crearlo antes de poder cerrar este requerimiento).
  - [x] 6.3 `dotnet test tests/SolarUY.Domain.Tests`. **Hecho: 109 pasados, 0 fallados (74 ms).**
  - [ ] 6.4 Analizar con SonarQube (`tools/SonarQube/analizar.sh .`; en Windows, `.\tools\SonarQube\analizar.ps1 .`) y corregir los issues nuevos *Blocker* o *Critical*.
  - [ ] 6.5 Criterio 1: el Administrador ve «Consultas» en el menú y entra a la pantalla; sin sesión o sin ese rol no accede.
  - [ ] 6.6 Criterio 2: dos preguntas seguidas; la segunda no usa el contexto de la primera.
  - [ ] 6.7 Criterio 3: «¿cómo está funcionando todo hoy?» responde con parques, paneles por estado y generación de hoy (repetir con cuota de Gemini disponible; también valida el caso normal de la 4.17).
  - [ ] 6.8 Criterio 3: la generación histórica de un parque por día, mes y año con rango de fechas coincide con la suma de la pantalla *Histórico* de sus paneles.
  - [ ] 6.9 Criterio 3: forzar una falla en el simulador y preguntar por los paneles en falla; aparece el panel y su nodo.
  - [ ] 6.10 Criterio 4: preguntar por un parque que no existe o sin lecturas; la respuesta lo dice y no inventa valores.
  - [ ] 6.11 Criterio 5: pedir «desactivá el panel P01» y una pregunta ajena al sistema; ambas se rechazan y el simulador no cambia.
  - [ ] 6.12 Criterio 6: pregunta vacía muestra validación; con `docker compose stop mcp` o una clave inválida aparece el aviso y el resto del sistema sigue andando.
  - [x] 6.13 Criterio 7: `git grep` de la clave de Gemini no encuentra nada versionado. **Hecho: ningún valor versionado; solo nombres de variables vacías y dos falsos positivos en `jquery*.min.map`.**
  - [ ] 6.14 El servidor MCP no es alcanzable desde el host ni a través de nginx (sin puertos publicados).
  - [ ] 6.15 Completar la sección `## Uso de IA` (ver regla `6_flujo_trabajo_ia.md`, sección 9).

## Uso de IA
*(Completar al cerrar el requerimiento: qué generó el agente, qué corrigió o reescribió la persona, qué detectaron los tests de arquitectura, SonarQube o la revisión, y cómo se resolvió.)*
