# SolarUY — Análisis de calidad con SonarQube

| | |
|---|---|
| **Proyecto** | SolarUY — gestión de parques solares y telemetría |
| **Materia** | Taller de Programación .NET 2026 — UTEC |
| **Repositorio** | `github.com/6buriano/.NetSolarUY` |
| **Carpeta** | `tools/SonarQube/` |
| **Actualizado** | 09/11/2026 |

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
   │  2. sonarscanner begin  →  3. dotnet build SolarUY.slnx  →  4. sonarscanner end
   │     (el escáner se engancha al compilador para analizar el código)
   ▼
servidor SonarQube  (http://localhost:9000, contenedor aparte con su Postgres)
      5. guarda el resultado y calcula el veredicto del Quality Gate
```

Todo corre en Docker: no hace falta instalar Java, SonarScanner ni nada en la máquina.

| Archivo | Qué hace |
|---|---|
| `docker-compose.yml` | Levanta el servidor SonarQube (edición Community) y su base Postgres. Solo escucha en `127.0.0.1:9000`. |
| `scanner/Dockerfile` | Imagen del analizador: SDK de .NET 10, Java (lo pide SonarScanner) y `dotnet-sonarscanner`. |
| `scanner/entrypoint.sh` | Lo que corre dentro del contenedor: `begin`, `dotnet build` y `end`. Acá están las exclusiones. |
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
(seguridad, confiabilidad o mantenibilidad). El Quality Gate mira esa cualidad, no el nombre del tipo: un issue
que SonarQube llama *code smell* puede afectar la confiabilidad y hacer fallar el gate.

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

*Agregar una fila después de cada análisis relevante.*

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

## 9. Limitaciones

- **Sin cobertura de pruebas, y por eso el gate no puede pasar.** El análisis no ejecuta las pruebas, así que la
  cobertura figura en 0 %. Como el gate exige `new_coverage` de al menos 80 % sobre el código nuevo, esa
  condición falla siempre: el gate no va a pasar hasta que se recoja cobertura o el equipo decida cambiar la
  condición.
- **Local y por persona.** Cada integrante tiene su propio servidor, token y resultados: no hay un SonarQube compartido.
- **En el CI es opcional.** El workflow `.github/workflows/sonar.yml` ya está escrito, pero se saltea mientras el
  repositorio no tenga la variable `SONAR_ENABLED` y los secretos `SONAR_HOST_URL`/`SONAR_TOKEN` apuntando a un
  servidor alcanzable desde el runner.
- **El gate es estricto sobre todo el código**, no solo sobre el código nuevo de cada cambio.
- **No cubre la arquitectura.** Las reglas de capas y nombres las verifica `tests/CodeQuality.Tests` (ArchUnitNET),
  que ya existe y corre en el CI con el resto de las pruebas.

## 10. Próximos pasos

1. Activar `sonar.yml`: variable `SONAR_ENABLED` y los secretos `SONAR_HOST_URL`/`SONAR_TOKEN`.
2. Sumar cobertura de pruebas al análisis, para que el gate pueda pasar.
3. Corregir los issues de los grupos A a E.
4. Reducir la complejidad de los métodos señalados.
