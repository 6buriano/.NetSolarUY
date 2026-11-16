# SolarUY — Análisis de calidad con SonarQube

| | |
|---|---|
| **Proyecto** | SolarUY — gestión de parques solares y telemetría |
| **Materia** | Taller de Programación .NET 2026 — UTEC |
| **Repositorio** | `github.com/6buriano/.NetSolarUY` |
| **Carpeta** | `tools/SonarQube/` |
| **Actualizado** | 12/11/2026 |

## 1. Para qué sirve

SonarQube es el análisis estático del proyecto: **lee el código sin ejecutarlo** y lo compara contra un conjunto
de reglas. Detecta bugs, vulnerabilidades, código duplicado y métodos demasiado complejos. No reemplaza a las
pruebas automáticas ni a los tests de arquitectura: las pruebas dicen si el código *funciona*, SonarQube dice si
está *bien escrito*.

Encaja en el proceso de desarrollo con IA (ver `README.md`, sección "Cómo desarrollamos con IA") como uno de los
**guardarraíles deterministas**: las reglas de `.agents/rules/` guían a la IA de forma probabilística, y SonarQube
comprueba el resultado con reglas fijas. Es el paso 4 de la skill `verificar-calidad`: al cerrar un requerimiento,
el Quality Gate tiene que pasar.

## 2. Cómo funciona

```
analizar.ps1 / analizar.sh
   │  1. comprueba que el servidor responda y crea el Quality Gate "EstrictoTaller"
   ▼
contenedor "solaruy-scanner"  (SDK de .NET 10 + Java + dotnet-sonarscanner)
   │  2. sonarscanner begin  →  3. dotnet build SolarUY.slnx  →  4. dotnet test (con cobertura)
   │     →  5. sonarscanner end
   │     (el escáner se engancha al compilador para analizar el código; las pruebas aportan la cobertura)
   ▼
servidor SonarQube  (http://localhost:9000, contenedor aparte con su Postgres)
      6. guarda el resultado y calcula el veredicto del Quality Gate
```

Todo corre en Docker: no hace falta instalar Java, SonarScanner ni nada en la máquina.

| Archivo | Qué hace |
|---|---|
| `docker-compose.yml` | Levanta el servidor SonarQube (edición Community) y su base Postgres. Solo escucha en `127.0.0.1:9000`. |
| `scanner/Dockerfile` | Imagen del analizador: SDK de .NET 10, Java (lo pide SonarScanner) y `dotnet-sonarscanner`. Corre como usuario `scanner`, no como `root`. |
| `scanner/entrypoint.sh` | Lo que corre dentro del contenedor: copia el código a `/tmp`, `begin`, `dotnet build`, `dotnet test` con cobertura y `end`. Acá están las exclusiones. |
| `analizar.ps1` / `analizar.sh` | Lo que se ejecuta desde la máquina: valida, configura el Quality Gate y lanza el contenedor. |

## 3. Primera vez

Requisitos: Docker funcionando y el submódulo del simulador inicializado (el análisis compila la solución, que
lo referencia).

**1. Levantar el servidor.** Tarda uno o dos minutos la primera vez:

```powershell
docker compose -f tools/SonarQube/docker-compose.yml up -d
curl.exe -s http://localhost:9000/api/system/status      # tiene que decir "status":"UP"
```

**2. Traer el submódulo**, si no se hizo al clonar:

```powershell
git submodule update --init
```

**3. Iniciar sesión.** Abrir `http://localhost:9000` con usuario `admin` y clave `admin`. SonarQube pide cambiar
la clave: elegir una y anotarla.

**4. Generar el token.** En **My Account → Security → Generate Tokens**, tipo *User Token*. Se muestra una sola
vez. **No se escribe en ningún archivo ni se sube al repo**: cada integrante genera el suyo.

**5. Analizar**, desde la raíz del repo:

```powershell
# Solo si PowerShell bloquea los scripts (vale hasta cerrar la ventana):
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

$env:SONAR_TOKEN = "el_token"
.\tools\SonarQube\analizar.ps1 .
```

En Linux o Mac:

```bash
SONAR_TOKEN=el_token ./tools/SonarQube/analizar.sh .
```

La primera corrida construye la imagen del analizador y descarga paquetes, así que tarda varios minutos.

## 4. Uso diario

| Quiero… | Comando |
|---|---|
| Analizar de nuevo | Paso 5 (con `SONAR_TOKEN` definido en la sesión) |
| Levantar el servidor si está apagado | `docker compose -f tools/SonarQube/docker-compose.yml up -d` |
| Apagarlo conservando los datos | `docker compose -f tools/SonarQube/docker-compose.yml stop` |
| Usar otro nombre de proyecto | `$env:PROJECT_KEY = "OtroNombre"` antes de analizar (por defecto `SolarUY`) |
| Empezar de cero | `docker compose -f tools/SonarQube/docker-compose.yml down -v` (borra proyectos, usuarios y claves) |

El script termina con **error si el Quality Gate no pasa**. Es a propósito, para que sirva de guardarraíl.

## 5. Cómo leer los resultados

Entrar a `http://localhost:9000/dashboard?id=SolarUY`:

- **Overview**: el veredicto *Passed* o *Failed* del Quality Gate, con la condición que falló.
- **Issues**: los problemas, con el archivo y la línea. En cada uno, la pestaña **Why is this an issue?**
  explica la regla y cómo corregirla.
- **Measures**: métricas del proyecto completo, como duplicación.

Cada issue tiene un **tipo** (bug, vulnerabilidad, code smell) y un **impacto** sobre una cualidad del software
(seguridad, confiabilidad o mantenibilidad). Ojo: el panel muestra la calificación de cada cualidad según el
impacto, pero la condición del gate puede no coincidir con ella. El 12/11 el panel mostraba confiabilidad C
(11 issues de impacto *Medium*) y el gate no falló por eso: solo falló la calificación de seguridad (ver 7.1).

### Quality Gate "EstrictoTaller"

Lo crea `analizar` con las condiciones del taller y lo deja como predeterminado del servidor local:

| Condición | Falla si… |
|---|---|
| `new_violations` | hay más de 0 issues nuevos |
| `new_coverage` | la cobertura del código nuevo es menor a 80 % |
| `new_duplicated_lines_density` | la duplicación del código nuevo supera el 3 % |
| `blocker_violations` | hay algún issue *Blocker* |
| `security_rating` | es peor que A |
| `reliability_rating` | es peor que A |

Las dos calificaciones se miden sobre **todo el código** del proyecto; las tres primeras, sobre el **código
nuevo** de cada cambio. Las calificaciones del gate (`security_rating`, `reliability_rating`) dependen de los
issues de tipo vulnerabilidad y *bug*: con 0 vulnerabilidades y 0 *bugs* quedan en A. Un solo problema de esos
tipos, aunque sea menor, alcanza para que el gate falle. La condición de *hotspots* revisados no está en el gate
que se ve en el servidor (se comprobó el 15/11 con la API); los *hotspots* se revisan igual en el panel.

### Qué se analiza y qué no

Las reglas son las del perfil por defecto de SonarQube ("Sonar way") para cada lenguaje. No las escribió el
equipo. Se excluyen estas rutas, definidas en `scanner/entrypoint.sh`:

| Exclusión | Motivo |
|---|---|
| `**/Migrations/**` | Código que genera EF Core. |
| `**/wwwroot/lib/**` | Bootstrap y jQuery de la plantilla MVC. |
| `SolarUY.SensorSimulator/**` | El mock del hardware: es otro repositorio (submódulo). |

## 6. Decisiones tomadas

El entorno parte del que provee la cátedra (`ControlCalidad/SonarQube` del repositorio del taller), con estos
cambios:

| Decisión | Por qué |
|---|---|
| El analizador corre como usuario `scanner`, no como `root` | Sonar marcaba `docker:S6471` (la imagen corría como `root`), el único issue de seguridad y lo que dejaba la calificación en B. (15/11/2026) |
| El contenedor analiza una **copia** del código en `/tmp` | La carpeta del repo montada desde Windows llega como propiedad de `root` y el usuario `scanner` no puede escribir ahí (`.sonarqube`, `bin`, `obj`). Copiar evita dar permisos de más y deja el repo sin archivos generados. Se excluyen `bin`, `obj`, `.sonarqube` y `node_modules` de la copia. (15/11/2026) |
| El token se pasa por variable de entorno | La regla 7 de `.agents/rules/6_flujo_trabajo_ia.md` prohíbe secretos en archivos versionados. |
| El contenedor llega al servidor por `host.docker.internal` | `--network host` puede no funcionar en Docker Desktop para Windows; esta forma sirve en Windows, Mac y Linux. |
| `sonar.token` en lugar de `sonar.login` | `sonar.login` está obsoleto. |
| `sonar.qualitygate.wait=true` | El análisis espera el veredicto y falla si el gate no pasa. |
| Servidor solo en `127.0.0.1` | Es una herramienta de desarrollo: no se expone a la red. |
| `sed` de finales de línea en el Dockerfile | Si el repo se clona en Windows con CRLF, `bash` no puede ejecutar `entrypoint.sh`. |
| Las pruebas corren dentro del análisis, con cobertura en formato OpenCover | Sin ellas SonarQube no recibe datos y muestra 0 % de cobertura aunque haya pruebas. Si una prueba falla, el análisis se corta: no tiene sentido evaluar la calidad de código que no pasa sus pruebas. (12/11/2026) |
| Los reportes de cobertura se guardan en `/tmp` del contenedor | No ensucian el repo con carpetas generadas. |

## 7. Primer análisis (01/11/2026)

El primer análisis completo terminó con el **Quality Gate en *Failed***: confiabilidad en C, seguridad en B y
cero issues *Blocker*. Los 23 issues que lo causan son de estos tipos:

| Grupo | Qué es | Issues | Corrección prevista |
|---|---|---|---|
| A. Etiquetas de formularios | `<label>` sin asociar a su campo en Histórico y Mantenimientos (accesibilidad). | 12 | `<label for>` + `id` en el campo. |
| B. Código bloqueante al arrancar | `Migrate()` y `Run()` en `Program.cs` de Web y Collector. | 4 | `MigrateAsync()` y `RunAsync()`. |
| C. Campos que pueden faltar | Identificadores `Guid` posteados desde formularios: si no llegan, se asigna `Guid.Empty` sin error. | 3 | `[BindRequired]` o hacerlos anulables. **Decisión pendiente**, toca módulos de otros integrantes. |
| D. Dockerfile del analizador | La imagen corre como `root`. | 1 | Usuario no-root. |
| E. Script de la entrega | Expresiones regulares lentas en `.entrega-build/generar-docx.py`. | 3 | Corregirlas o excluir `.entrega-build/**`. **Decisión pendiente.** |

Además hay avisos de **complejidad cognitiva** (por ejemplo en `ComparadorVinculacion.cs`). Son de
mantenibilidad y no hacen fallar el gate, pero quedan como mejora.

Ningún hallazgo rompe el funcionamiento del sistema. Las correcciones van en el código: no se marcan issues
como *Won't fix* ni *False positive* sin explicar el motivo y que lo apruebe el equipo.

### Registro de análisis

| Fecha | Quality Gate | Confiabilidad | Seguridad | Notas |
|---|---|---|---|---|
| 01/11/2026 | Failed | C | B | Primer análisis, 23 issues (ver arriba). |
| 09/11/2026 | Failed | C | B | Tras req011: 23 issues de confiabilidad/seguridad, todos de los grupos A–E (A 12, B 4, C 3, D 1, E 3); req011 no agrega ninguno. 7 issues nuevos de mantenibilidad (Low/Info); cobertura nueva 0 % (limitación conocida). |
| 12/11/2026 | Failed | C | B | Primer análisis **con cobertura**: 16,7 % sobre código nuevo. 72 issues abiertos (21 nuevos): 11 de confiabilidad, 1 de seguridad, 60 de mantenibilidad. Se corrigió el grupo A (13 issues de etiquetas). Detalle en la sección 7.1. |
| 15/11/2026 | Failed | C | A | Escáner sin `root` (grupo D corregido): la seguridad pasa a A. El panel sigue mostrando confiabilidad C (11 issues de impacto *Medium*), pero la condición del gate (`reliability_rating`) está en A. 70 issues abiertos (21 nuevos), 0 *bugs*, 0 vulnerabilidades. Solo fallan `new_coverage` (16,7 %) y `new_violations` (21). Detalle en la sección 7.2. |
| 15/11/2026 (2.º) | Failed | A (gate) | A | Grupo B corregido: `MigrateAsync()` y `RunAsync()` en Web y Collector. 66 issues abiertos (21 nuevos), 0 *bugs*, 0 vulnerabilidades; las 216 pruebas siguen pasando. Fallan las mismas dos condiciones: `new_coverage` (16,7 %) y `new_violations` (21). Detalle en la sección 7.3. |
| 15/11/2026 (3.º) | Failed | A (gate) | A | Se corrigieron 8 de los 21 issues nuevos (`S1192`, `CA1861`, `CA1826`, `S1450`). 58 issues abiertos (13 nuevos), 0 *bugs*, 0 vulnerabilidades; las 216 pruebas pasan. Fallan las mismas dos condiciones: `new_coverage` (16,5 %) y `new_violations` (13). Detalle en la sección 7.4. |
| 15/11/2026 (4.º) | Failed | A (gate) | A | Pruebas nuevas de `MantenimientoService`, `ReporteService`, `ConsultaService` y `MonitoreoService` (41 pruebas, 257 en total): `new_coverage` sube de 17,4 % a 26,2 % y la cobertura total de 15,0 % a 18,0 %. 58 issues abiertos (13 nuevos). Fallan `new_coverage` y `new_violations` (13). Detalle en la sección 7.5. |
| 15/11/2026 (5.º) | Failed | A (gate) | A | Pruebas nuevas de `RetencionTelemetriaWorker`, `ConsultasController` y `MantenimientosController` (41 pruebas, 298 en total, en dos proyectos de prueba nuevos): `new_coverage` sube de 26,2 % a 39,1 % y la cobertura total de 18,0 % a 25,0 %. 58 issues abiertos (13 nuevos). Fallan `new_coverage` y `new_violations` (13). Detalle en la sección 7.6. |

*Agregar una fila después de cada análisis relevante.*

### 7.1 Análisis del 12/11/2026: primero con cobertura

El Quality Gate sigue en *Failed*, ahora con los números reales. En **código nuevo** (desde el 01/11) fallan dos
condiciones; duplicación y *hotspots* pasan:

| Condición | Resultado | Exigido |
|---|---|---|
| Issues nuevos | 21 (eran 22 antes de corregir las etiquetas) | 0 |
| Cobertura nueva | 16,7 %, sobre 884 líneas a cubrir | ≥ 80 % |
| Duplicación nueva | 0,0 % | ≤ 3 % |
| *Hotspots* nuevos | ninguno (calificación A) | no es condición del gate |

La cobertura y la duplicación se midieron en la corrida anterior de ese mismo día; el cambio posterior solo tocó
vistas `.cshtml`, que no cuentan como líneas a cubrir.

En **todo el código** hay 72 issues abiertos: 11 de confiabilidad (todos *Medium*), 1 de seguridad (*Low*) y 60 de
mantenibilidad. El panel muestra confiabilidad **C**, seguridad **B** y mantenibilidad **A** (la calificación la fija
el issue más grave que quede abierto). La cobertura total es de 13,0 % sobre 3,9 mil líneas a cubrir y la
duplicación, de 0,0 % sobre 10 mil líneas.

Sobre todo el código, el gate falla por **una sola condición: la calificación de seguridad (B)**. La causa es el
único issue de seguridad abierto: la imagen del analizador corre como `root` (grupo D). La confiabilidad figura en C
y aun así no hace fallar el gate. Por eso, de los grupos de abajo, solo el D sostiene hoy el fallo de calificación.
Los grupos A, B, C y E mejoran la confiabilidad del código, pero solo pesan en el gate a través de la condición de
issues nuevos cuando son de código nuevo (hoy, `CierreMantenimientoViewModel`).

**Estado de los grupos de la sección 7:**

| Grupo | Estado |
|---|---|
| A. Etiquetas de formularios | **Corregido el 12/11**: 13 issues, incluida la del selector de parque de Monitoreo. |
| B. Código bloqueante al arrancar | **Corregido el 15/11**: `MigrateAsync()` y `RunAsync()` en `Program.cs` de Web y Collector (ver 7.3). |
| C. Campos que pueden faltar | Pendiente (4): se sumó `CierreMantenimientoViewModel`. |
| D. Dockerfile del analizador | **Corregido el 15/11**: el escáner corre como usuario `scanner` (ver 7.2). |
| E. Script de la entrega | Pendiente (3): expresiones regulares de `generar-docx.py`. |

**Los 21 issues nuevos**, por tipo:

| Tipo | Cant. | Qué es |
|---|---|---|
| `S1135` | 7 | **Falsos positivos por el idioma** (ver abajo). |
| `CA1861` | 3 | Arrays constantes en pruebas de `MonitoreoServiceTests`. |
| `S1192`, `S1450` | 4 | Literal repetido (`MensajeExito`/`MensajeError`) y campos que pueden ser variables locales. |
| `CA1873` | 2 | Argumentos de log que se evalúan aunque el log esté apagado. |
| `S6964` | 1 | `CierreMantenimientoViewModel`: campo que puede llegar vacío (grupo C). |
| `S3776`, `S101` | 2 | Complejidad cognitiva de `SeguimientoMonitoreo` (18 de 15) y nombre `IMCPAgentService`. |
| `CA1826`, `S6667` | 2 | Método de `Enumerable` sobre lista indexable y `catch` que no pasa la excepción al log. |

**Falsos positivos de `S1135` por el idioma.** La regla busca la palabra `TODO` sin distinguir mayúsculas, y en
castellano "todo" es una palabra común ("todo el sistema", "todo lo que no cierra"). De los 15 issues `S1135` del
proyecto, 14 son comentarios de ese tipo: se revisó línea por línea. El único `TODO` real es el de `Program.cs`
línea 117, ligado a la etapa de seguridad (Keycloak). No se marcaron como *False positive* ni se desactivó la regla:
el tratamiento (reescribir los comentarios, marcarlos o ajustar el perfil) queda como decisión del equipo.

### 7.2 Análisis del 15/11/2026: escáner sin `root`

Se corrigió el grupo D. El Quality Gate sigue en *Failed*, pero ahora **solo por dos condiciones del código nuevo**;
las calificaciones sobre todo el código ya cumplen:

| Condición | Resultado | Exigido | Estado |
|---|---|---|---|
| `security_rating` | A | A | OK (antes B) |
| `reliability_rating` | A (0 *bugs*) | A | OK |
| `blocker_violations` | 0 | 0 | OK |
| `new_duplicated_lines_density` | 0,0 % | ≤ 3 % | OK |
| `new_coverage` | 16,7 % | ≥ 80 % | **Falla** |
| `new_violations` | 21 | 0 | **Falla** |

Medidas sobre todo el código: 70 issues abiertos (11 de confiabilidad y 59 de mantenibilidad, según el panel), 0
*bugs*, 0 vulnerabilidades, 0 *hotspots*, cobertura total 13,0 %, duplicación 0,0 % y 7.546 líneas de código. Las 216
pruebas pasan (115 de Domain, 20 de Application, 70 del simulador y 11 de calidad de arquitectura).

Respecto del 12/11 hay 2 issues menos (72 → 70), entre ellos `docker:S6471`; los 21 nuevos siguen siendo 21
(sección 7.1). Siguen abiertos `S6966` ×4, `S6964` ×4 y `S6562` ×3, que suman los 11 issues de confiabilidad de
impacto *Medium* que el panel sigue mostrando (confiabilidad C). El gate no los toma en cuenta, porque su
`reliability_rating` está en A con 0 *bugs*. Es la misma diferencia entre panel y gate que se explica en la sección 5.

Cómo se llegó, y qué decisiones quedan: el usuario sin `root` obligó a analizar sobre una copia en `/tmp` (ver
sección 6). Para pasar el gate sobre código nuevo faltan las dos condiciones de arriba: corregir los 21 issues
nuevos (los 7 `S1135` son falsos positivos por el idioma, ver 7.1) y subir la cobertura nueva, o ajustar esas
condiciones con el equipo.

### 7.3 Análisis del 15/11/2026 (segundo): grupo B

Se corrigió el grupo B: las 4 llamadas bloqueantes del arranque pasaron a su versión asíncrona
(`Database.MigrateAsync()` en el modo `--migrate-only` y en el arranque de desarrollo de Web, `app.RunAsync()` en
Web y `host.RunAsync()` en Collector). Ambos `Program.cs` ya usaban instrucciones de nivel superior, así que
alcanzó con agregar `await`; el comportamiento al arrancar no cambia, solo deja de bloquearse un hilo mientras se
espera. Los 4 issues `S6966` desaparecieron: de 70 a 66 issues abiertos.

El gate no cambia: siguen fallando `new_coverage` (16,7 %) y `new_violations` (21), porque los 4 issues corregidos
eran de código viejo. Los 4 pertenecían a los 11 issues de confiabilidad de impacto *Medium*; quedan 7
(`S6964` ×4 y `S6562` ×3). Cobertura total 13,0 %, duplicación 0,0 %, 7.546 líneas de código.

### 7.4 Análisis del 15/11/2026 (tercero): issues nuevos sencillos

Se corrigieron 8 de los 21 issues nuevos, con cambios que no alteran el comportamiento (compilan y las 216 pruebas
pasan):

| Regla | Cant. | Cambio |
|---|---|---|
| `S1192` | 2 | `MantenimientosController`: constantes para las claves de `TempData` que lee `_Layout.cshtml`. |
| `CA1861` | 3 | `MonitoreoServiceTests`: los arrays de las comparaciones pasan a campos `static readonly`. |
| `CA1826` | 1 | `MonitoreoController`: `Count > 0 ? [0] : null` en lugar de `FirstOrDefault()` sobre una lista. |
| `S1450` | 2 | `AgenteNoConfigurado`: los campos solo se usaban en el constructor. |

Resultado: de 66 a 58 issues abiertos y de 21 a 13 nuevos. La cobertura nueva figura en 16,5 % (era 16,7 %): se
agregaron algunas líneas sin pruebas propias. El gate sigue fallando por `new_coverage` y `new_violations`.

**Los 13 issues nuevos que quedan:**

| Regla | Cant. | Situación |
|---|---|---|
| `S1135` | 7 | Falsos positivos por el idioma (sección 7.1). Decisión del equipo. |
| `CA1873` | 2 | `RetencionTelemetriaWorker`: guardar los valores en variables locales **no** alcanzó, la regla sigue marcando las dos llamadas de log. Falta probar con `if (_logger.IsEnabled(...))` o `[LoggerMessage]`. |
| `S6667` | 1 | `GeminiMcpAgentService`: ver abajo. |
| `S3776` | 1 | `SeguimientoMonitoreo`: complejidad 18 de 15. Requiere refactor. |
| `S101` | 1 | Renombrar `IMCPAgentService` a `IMcpAgentService`: toca varios módulos, hay que acordarlo. |
| `S6964` | 1 | `CierreMantenimientoViewModel`: grupo C, decisión pendiente. |

**`S6667` en `GeminiMcpAgentService` (propuesta, pendiente de aprobación del equipo).** El código registra solo el
tipo de la excepción, no la excepción completa, y es deliberado: el mensaje de una excepción HTTP puede incluir la
URL de la solicitud, y ahí puede ir la clave de la API del proveedor. Pasar la excepción al log para cumplir la
regla arriesgaría filtrar un secreto. Se propone dejar el issue abierto y documentado como decisión justificada,
sin marcarlo como *False positive* hasta que el equipo lo apruebe.

### 7.5 Análisis del 15/11/2026 (cuarto): pruebas de `Application`

Se escribieron pruebas unitarias para cuatro servicios de `Application`, con repositorios falsos en memoria (el
proyecto no usa librerías de mocks): `MantenimientoService` (20 pruebas), `ReporteService` (10),
`ConsultaService` (8) y `MonitoreoService.ObtenerInstantaneaAsync` (3). Las 257 pruebas pasan.

| Medida | Antes | Ahora |
|---|---|---|
| `new_coverage` (código nuevo) | 17,4 % | **26,2 %** |
| Cobertura total | 15,0 % | 18,0 % |
| Issues abiertos / nuevos | 58 / 13 | 58 / 13 |

Un hallazgo que conviene tener presente: **las pruebas de `MantenimientoService` subieron la cobertura total pero
casi no movieron `new_coverage`** (de 16,5 % a 17,4 %), porque `new_coverage` solo cuenta el código nuevo o
modificado desde el período de referencia (01/11). Para ese número sirven las pruebas de lo que se tocó después.

**Qué falta cubrir del código nuevo.** Quedan 682 líneas nuevas sin cubrir en 35 archivos (según
`api/measures/component_tree`). Agrupadas:

| Zona | Líneas sin cubrir | Archivos principales |
|---|---|---|
| Agente y herramientas MCP | 236 | `GeminiMcpAgentService` (75), `ObtenerGeneracionHistoricaTool` (70), `ListarPanelesEnFallaTool` (26) |
| Web (controladores, worker, `Program.cs`) | 139 | `MonitoreoTiempoRealWorker` (44), `ConsultasController` (29), `MantenimientosController` (24) |
| JavaScript (`monitoreo.js`, `panel-detalle.js`) | 123 | El proyecto no tiene pruebas de front-end |
| Infraestructura (inyección, repositorio) | 88 | `DependencyInjection` (54), `TelemetriaRepository` (30) |
| Collector | 66 | `RetencionTelemetriaWorker` (59) |
| Application | 30 | `ParqueMappings` (12), `ResumenParqueDto` (9), `ParqueService` (7) |

El total de líneas nuevas a cubrir no sale directo del reporte: se estima en unas 924 (682 sin cubrir con 26,2 % de
cobertura). Con esa estimación, llegar al 80 % exigiría cubrir unas 500 líneas más, y la mayor parte está en zonas
que piden pruebas de integración o de infraestructura, no unitarias simples.

**Decisiones pendientes (del responsable del proyecto):** si se excluyen los archivos JavaScript de la medida de
cobertura (`sonar.coverage.exclusions`) por no haber pruebas de front-end; qué pruebas se escriben antes de la
defensa; y qué valor de `new_coverage` se defiende, con su justificación, en lugar del 80 %.

### 7.6 Análisis del 15/11/2026 (quinto): pruebas de `Collector` y de los controladores

Se crearon dos proyectos de prueba, que antes no existían: `tests/SolarUY.Collector.Tests` (8 pruebas de
`RetencionTelemetriaWorker`) y `tests/SolarUY.Web.Tests` (5 de `ConsultasController` y 28 de
`MantenimientosController`). Los dos están en `SolarUY.slnx`, así que el CI los corre con `dotnet test SolarUY.slnx`;
los Dockerfiles publican cada proyecto por separado y no se ven afectados. Las pruebas del worker usan el
`RetencionTelemetriaService` real dentro de un contenedor de servicios real y esperan con `TaskCompletionSource` en
lugar de dormir, para no depender de tiempos. Las 298 pruebas pasan.

| Medida | Antes | Ahora |
|---|---|---|
| `new_coverage` (código nuevo) | 26,2 % | **39,1 %** |
| Cobertura total | 18,0 % | 25,0 % |
| Issues abiertos / nuevos | 58 / 13 | 58 / 13 |

La estimación previa era de 36 a 38 %; el resultado real quedó algo por encima. `RetencionTelemetriaWorker` pasó de
59 a 2 líneas sin cubrir, y `ConsultasController` y `MantenimientosController` salieron por completo de la lista de
archivos con código nuevo sin cubrir.

**Qué falta cubrir del código nuevo.** Quedan 561 líneas nuevas sin cubrir (antes 682), según
`api/measures/component_tree`:

| Zona | Líneas sin cubrir | Archivos principales |
|---|---|---|
| Agente y herramientas MCP | 236 | `GeminiMcpAgentService` (75), `ObtenerGeneracionHistoricaTool` (70), `ListarPanelesEnFallaTool` (26) |
| JavaScript (`monitoreo.js`, `panel-detalle.js`) | 123 | El proyecto no tiene pruebas de front-end |
| Infraestructura (inyección, repositorio) | 88 | `DependencyInjection` (54), `TelemetriaRepository` (30) |
| Web (worker, `Program.cs`, otros) | 79 | `MonitoreoTiempoRealWorker` (44), `Program.cs` (18), `MonitoreoController` (10) |
| Application | 29 | `ParqueMappings` (12), `ResumenParqueDto` (9), `ParqueService` (7) |
| Collector | 6 | `Program.cs` (4), `RetencionTelemetriaWorker` (2) |

**Cuánto falta para el 80 %.** El total de líneas nuevas a cubrir se estima en unas 920 (561 sin cubrir con 39,1 %
de cobertura); es una estimación, no un dato del reporte. Con ella, llegar al 80 % exige cubrir unas 380 líneas más.
Si se excluyeran los dos archivos JavaScript de la medida, el total bajaría a unas 800 líneas, la cobertura actual
equivaldría a un 45 % aproximado y faltarían unas 280 líneas. En ambos casos la mayor parte está en el agente y las
herramientas MCP (que dependen de un proveedor externo o de acceso a datos) y en `Infrastructure`, que piden pruebas
con dobles de HTTP o de base de datos, bastante más costosas que las pruebas unitarias de los servicios.

**Decisiones pendientes (del responsable del proyecto):** si se excluyen los archivos JavaScript de la medida de
cobertura (`sonar.coverage.exclusions`); si se escriben pruebas de las herramientas MCP antes de la defensa; y qué
valor de `new_coverage` se defiende, con su justificación, en lugar del 80 %.

## 8. Problemas típicos

| Síntoma | Causa | Qué hacer |
|---|---|---|
| "La ejecución de scripts está deshabilitada en este sistema" | Política de ejecución de PowerShell. | `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass` y repetir. |
| "SonarQube no responde en http://localhost:9000" | El servidor no está levantado o aún arranca. | Levantarlo (sección 4) y esperar a que `/api/system/status` diga `UP`. |
| "defini `$env:SONAR_TOKEN`" | La variable no está en esta sesión de PowerShell. | Volver a asignarla; se pierde al cerrar la terminal. |
| El build falla dentro del contenedor | Submódulo del simulador sin inicializar. | `git submodule update --init`. |
| El puerto 9000 está ocupado | Hay otro SonarQube corriendo. | Detenerlo o usar ese mismo servidor (los scripts solo hablan con `localhost:9000`). |
| `admin` / `admin` no entra | La clave ya se había cambiado. | Si no hay nada que conservar, `down -v` y empezar de cero. |
| Quality Gate *Failed* | Hay issues que superan lo permitido. | Leer **Overview** e **Issues** y corregir en el código. |
| Avisos de "blame" de archivos del simulador | El submódulo tiene su propio git. | Se pueden ignorar. |
| Cambié `entrypoint.sh` y el análisis sigue haciendo lo de antes | `analizar` solo construye la imagen si no existe, y el `entrypoint.sh` va copiado adentro de ella. | `docker build -t solaruy-scanner tools/SonarQube/scanner` y repetir. |
| `Access to the path '/app/.sonarqube/...' is denied` | El contenedor corre sin `root` y analiza directo sobre la carpeta montada, que no es escribible. | Usar el `entrypoint.sh` que copia el código a `/tmp`, reconstruir la imagen y repetir. |
| El análisis se corta en "Ejecutando pruebas con cobertura" | Hay una prueba fallando. | Correr `dotnet test SolarUY.slnx` en la máquina y corregir la prueba. |

## 9. Limitaciones

- **La cobertura se mide, pero está lejos del umbral.** Desde el 12/11 el análisis ejecuta las pruebas y envía la
  cobertura (antes figuraba en 0 %). El 12/11 el resultado era 16,7 % sobre el código nuevo; tras las pruebas del
  15/11 es 39,1 %, contra el 80 % que exige el gate (secciones 7.5 y 7.6). Hoy el dominio, varios servicios de
  `Application`, el worker de retención y dos controladores están probados, pero el agente y las herramientas MCP,
  `Infrastructure` y el JavaScript casi no tienen pruebas, y ahí están la mayoría de las líneas nuevas sin cubrir. Hay dos caminos y los dos son decisión del
  responsable: escribir más pruebas, o ajustar la condición `new_coverage` con un umbral alcanzable y justificado.
- **La exclusión del simulador no alcanza a sus pruebas.** `SolarUY.SensorSimulator/**` deja afuera el código del
  simulador, pero dos issues (`CA1861`) de `SolarUY.SensorSimulator.Tests` siguen apareciendo. La causa probable es
  que el escáner de .NET resuelve las exclusiones respecto de la carpeta de cada proyecto. No afecta al gate: no son
  código nuevo ni de confiabilidad o seguridad.
- **Local y por persona.** Cada integrante tiene su propio servidor, token y resultados: no hay un SonarQube compartido.
- **En el CI es opcional.** El workflow `.github/workflows/sonar.yml` ya está escrito, pero se saltea mientras el
  repositorio no tenga la variable `SONAR_ENABLED` y los secretos `SONAR_HOST_URL`/`SONAR_TOKEN` apuntando a un
  servidor alcanzable desde el runner.
- **El gate es estricto sobre todo el código**, no solo sobre el código nuevo de cada cambio.
- **No cubre la arquitectura.** Las reglas de capas y nombres las verifica `tests/CodeQuality.Tests` (ArchUnitNET),
  que ya existe y corre en el CI con el resto de las pruebas.

## 10. Próximos pasos

1. Activar `sonar.yml`: variable `SONAR_ENABLED` y los secretos `SONAR_HOST_URL`/`SONAR_TOKEN`.
2. Decidir con el equipo qué hacer con la condición de cobertura: más pruebas o un umbral justificado.
3. Pruebas de `Application`: ya están `Mantenimiento`, `Reporte`, `Consulta` y `Monitoreo`. También están el worker de
   retención y los controladores `Consultas` y `Mantenimientos`. Faltan `Nodo`, `Panel`, `Parque` y
   `VerificacionVinculacion`, las herramientas MCP y `MonitoreoTiempoRealWorker`; para `new_coverage` pesan más los
   archivos tocados desde el 01/11 (secciones 7.5 y 7.6).
4. Los grupos A, B y D ya están corregidos (el D dejó la seguridad en A). Sigue el grupo C (campos que pueden
   faltar, `S6964`), que toca módulos de otros integrantes y hay que acordar con el equipo; mejora la confiabilidad
   aunque hoy no haga fallar el gate.
5. Decidir cómo tratar los falsos positivos de `S1135` (sección 7.1).
6. Reducir la complejidad de los métodos señalados (`S3776`).
