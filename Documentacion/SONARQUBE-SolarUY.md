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
| `scanner/Dockerfile` | Imagen del analizador: SDK de .NET 10, Java (lo pide SonarScanner) y `dotnet-sonarscanner`. |
| `scanner/entrypoint.sh` | Lo que corre dentro del contenedor: `begin`, `dotnet build`, `dotnet test` con cobertura y `end`. Acá están las exclusiones. |
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
| `new_security_hotspots_reviewed` | se revisó menos del 100 % de los *hotspots* nuevos |
| `blocker_violations` | hay algún issue *Blocker* |
| `security_rating` | es peor que A |
| `reliability_rating` | es peor que A |

Las dos calificaciones se miden sobre **todo el código** del proyecto; las cuatro primeras, sobre el **código
nuevo** de cada cambio. Una calificación A significa que **no queda ningún issue** de esa cualidad, de ninguna
severidad. Por eso un solo problema menor alcanza para que el gate falle.

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

*Agregar una fila después de cada análisis relevante.*

### 7.1 Análisis del 12/11/2026: primero con cobertura

El Quality Gate sigue en *Failed*, ahora con los números reales. En **código nuevo** (desde el 01/11) fallan dos
condiciones; duplicación y *hotspots* pasan:

| Condición | Resultado | Exigido |
|---|---|---|
| Issues nuevos | 21 (eran 22 antes de corregir las etiquetas) | 0 |
| Cobertura nueva | 16,7 %, sobre 884 líneas a cubrir | ≥ 80 % |
| Duplicación nueva | 0,0 % | ≤ 3 % |
| *Hotspots* nuevos | ninguno (calificación A) | 100 % revisados |

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
| B. Código bloqueante al arrancar | Pendiente (4): `Migrate()` y `Run()` en `Program.cs` de Web y Collector. |
| C. Campos que pueden faltar | Pendiente (4): se sumó `CierreMantenimientoViewModel`. |
| D. Dockerfile del analizador | Pendiente (1): corre como `root`. Es el único issue de seguridad. |
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
| El análisis se corta en "Ejecutando pruebas con cobertura" | Hay una prueba fallando. | Correr `dotnet test SolarUY.slnx` en la máquina y corregir la prueba. |

## 9. Limitaciones

- **La cobertura se mide, pero está lejos del umbral.** Desde el 12/11 el análisis ejecuta las pruebas y envía la
  cobertura (antes figuraba en 0 %). El resultado es 16,7 % sobre el código nuevo contra el 80 % que exige el gate:
  con 884 líneas a cubrir, harían falta unas 560 más cubiertas (estimación a partir de esos números). Hoy el
  dominio está bien probado, siete servicios de `Application` no tienen pruebas, y `Infrastructure`, `Web` y
  `Collector` tampoco. Hay dos caminos y los dos son decisión del equipo: escribir esas pruebas, o ajustar la
  condición `new_coverage` con un umbral alcanzable y justificado.
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
3. Escribir pruebas de los servicios de `Application` que no tienen (`Nodo`, `Panel`, `Parque`, `Mantenimiento`,
   `Reporte`, `Consulta`, `VerificacionVinculacion`): es lo que más cobertura aporta por línea escrita.
4. Corregir el grupo D (Dockerfile sin `root`): es lo único que sostiene la calificación de seguridad en B, que es
   hoy la única condición que falla sobre todo el código. Después los grupos B (arranque asíncrono) y C (campos que
   pueden faltar), que mejoran la confiabilidad. El A ya está corregido.
5. Decidir cómo tratar los falsos positivos de `S1135` (sección 7.1).
6. Reducir la complejidad de los métodos señalados (`S3776`).
