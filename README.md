# SolarUY

Taller de Programación .NET 2026, UTEC (Equipo 6).

## Requisitos para trabajar

- **.NET SDK 10** (comprobar con `dotnet --version`).
- Un editor: VS Code con la extensión **C# Dev Kit**, Rider o Visual Studio.
- Git.

## Estructura de la solución

```
.NetSolarUY/
├── SolarUY.slnx                  ← la solución: agrupa todos los proyectos
├── src/
│   ├── SolarUY.Domain/           ← entidades, enums, reglas del negocio
│   ├── SolarUY.Application/      ← servicios (casos de uso), DTOs y contratos externos
│   ├── SolarUY.Infrastructure/   ← base de datos (EF Core + MySQL), repositorios, cliente del sensor
│   ├── SolarUY.Web/              ← aplicación MVC: controladores y vistas
│   └── SolarUY.Collector/        ← colector de telemetría (Worker Service)
├── tests/
│   ├── SolarUY.Domain.Tests/     ← pruebas unitarias del dominio (xUnit)
│   └── ...
├── SolarUY.SensorSimulator/      ← SUBMÓDULO: mock de hardware (sensores ARM + inversores)
└── *.md                          ← reglas de desarrollo con IA y specs
```

`SolarUY.slnx` es el formato de solución de .NET 10 (antes era `.sln`).

⚠️ **`SolarUY.SensorSimulator` es un submódulo git** (repositorio aparte:
[github.com/RamosMariano/SolarUY.SensorSimulator](https://github.com/RamosMariano/SolarUY.SensorSimulator)).
Git no trae su contenido al clonar: después de clonar hay que inicializarlo, o `dotnet build` va a fallar
porque la solución referencia sus proyectos.

```bash
git clone --recurse-submodules https://github.com/6buriano/.NetSolarUY.git
# o, si ya clonaste sin la opción:
git submodule update --init
```

## Qué va en cada proyecto

| Proyecto | Tipo | Qué contiene | Ejemplo |
|---|---|---|---|
| `SolarUY.Domain` | Biblioteca de clases | Las entidades del diagrama de clases y sus reglas. Es C# puro, sin librerías externas. | `ParqueSolar`, `EstadoSalud` |
| `SolarUY.Application` | Biblioteca de clases | Los casos de uso: lo que el sistema hace. Usa interfaces para no depender de la base de datos. | `ParqueService`, `IParqueRepository`, `ISensorClient` |
| `SolarUY.Infrastructure` | Biblioteca de clases | Los detalles técnicos: cómo se guarda en MySQL y cómo se habla con el sensor por HTTP. | `SolarUYDbContext`, `SensorHttpClient` |
| `SolarUY.Web` | Aplicación MVC | Las pantallas. Recibe pedidos del navegador y llama a los servicios. | `ParquesController`, `Views/Parques/Index.cshtml` |
| `SolarUY.Collector` | Worker Service | Programa sin pantalla que consulta a los sensores y guarda la telemetría. | `ColectorTelemetriaWorker` |
| `SolarUY.SensorSimulator` | Minimal API (submódulo) | El mock del hardware: emula los sensores ARM y sus inversores. Es una caja negra, no conoce nuestro dominio. | `SensorSimulator/Endpoints` |
| `SolarUY.Domain.Tests` | Proyecto xUnit | Pruebas de la lógica del dominio. | `ParqueSolarTests`, `NodoEvaluarLecturasTests` |

## Quién puede usar a quién (referencias)

Cada proyecto solo puede usar el código de los proyectos que tiene como referencia. Así se cumple Clean Architecture: las dependencias apuntan siempre hacia el centro (Domain).

| Proyecto | Referencia a | Por qué |
|---|---|---|
| Domain | nada | Es el núcleo del negocio y no depende de nadie |
| Application | Domain | Los casos de uso trabajan con las entidades |
| Infrastructure | Application, Domain | Implementa las interfaces que pide Application (por ejemplo, guardar en MySQL) |
| Web | Application, Domain, Infrastructure | Es el punto de arranque de la web |
| Collector | Application, Domain, Infrastructure | Es el punto de arranque del colector de telemetría |
| Domain.Tests | Domain | Prueba las entidades |
| SensorSimulator | **nada** | Es el mock del hardware: a propósito no conoce nuestro dominio |

⚠️ **Web referencia a Infrastructure solo para `Program.cs`**, que es donde se conectan las interfaces con sus implementaciones (inyección de dependencias). **Los controladores nunca usan Infrastructure directamente** (ni el `DbContext` ni los repositorios concretos): siempre llaman a un servicio de Application. Lo mismo vale para el Collector.

## Cómo compilar y correr

Desde la carpeta raíz del repo:

```bash
dotnet build                                # compila toda la solución (incluye el submódulo)
dotnet run --project src/SolarUY.Web        # levanta la web (abrir la URL que muestra)
dotnet run --project src/SolarUY.Collector  # levanta el colector de telemetría
dotnet test                                 # corre las pruebas de todo
```

Para cortar un programa que está corriendo: `Ctrl + C` en la terminal.

### Base de datos (MySQL) y simulador del sensor

```bash
docker compose up -d        # levanta MySQL 8.4 en localhost:3306 y el simulador en localhost:5100
dotnet tool restore         # instala la herramienta dotnet-ef que está fijada en .config/dotnet-tools.json
```

El simulador es el mock del hardware: se levanta desde el submódulo, así que si no lo inicializaste
`docker compose up -d sensor-simulator` va a fallar. Para levantarlo solo a él:
`docker compose up -d sensor-simulator`.

La cadena de conexión de desarrollo está en `src/SolarUY.Web/appsettings.Development.json` y coincide con
las credenciales de `docker-compose.yml`. En desarrollo la web aplica las migraciones sola al arrancar,
así que normalmente no hace falta correr nada más.

Para aplicar las migraciones a mano, o para crear una nueva:

```bash
dotnet ef database update --project src/SolarUY.Infrastructure --startup-project src/SolarUY.Web

dotnet ef migrations add NombreDeLaMigracion --project src/SolarUY.Infrastructure --startup-project src/SolarUY.Web --output-dir Persistence/Migrations
```

⚠️ `migrations add` necesita MySQL levantado: Pomelo detecta la versión del servidor para generar el SQL.

## Cómo se creó la base

```bash
# 1. La solución
dotnet new sln -n SolarUY

# 2. Las capas (bibliotecas de clases)
dotnet new classlib -n SolarUY.Domain -o src/SolarUY.Domain
dotnet new classlib -n SolarUY.Application -o src/SolarUY.Application
dotnet new classlib -n SolarUY.Infrastructure -o src/SolarUY.Infrastructure

# 3. La web, el simulador y las pruebas
dotnet new mvc -n SolarUY.Web -o src/SolarUY.Web
dotnet new worker -n SolarUY.Collector -o src/SolarUY.Collector
dotnet new xunit -n SolarUY.Domain.Tests -o tests/SolarUY.Domain.Tests

# 4. Agregar los proyectos a la solución y crear el .gitignore
dotnet sln add src/*/*.csproj tests/*/*.csproj
dotnet new gitignore

# 5. Referencias entre proyectos (ver tabla de arriba)
dotnet add src/SolarUY.Application/SolarUY.Application.csproj reference src/SolarUY.Domain/SolarUY.Domain.csproj
dotnet add src/SolarUY.Infrastructure/SolarUY.Infrastructure.csproj reference src/SolarUY.Application/SolarUY.Application.csproj src/SolarUY.Domain/SolarUY.Domain.csproj
dotnet add src/SolarUY.Web/SolarUY.Web.csproj reference src/SolarUY.Application/SolarUY.Application.csproj src/SolarUY.Domain/SolarUY.Domain.csproj src/SolarUY.Infrastructure/SolarUY.Infrastructure.csproj
dotnet add src/SolarUY.Collector/SolarUY.Collector.csproj reference src/SolarUY.Application/SolarUY.Application.csproj src/SolarUY.Domain/SolarUY.Domain.csproj src/SolarUY.Infrastructure/SolarUY.Infrastructure.csproj
dotnet add tests/SolarUY.Domain.Tests/SolarUY.Domain.Tests.csproj reference src/SolarUY.Domain/SolarUY.Domain.csproj
```

Qué significa cada plantilla:

- `sln`: la solución, que agrupa los proyectos.
- `classlib`: biblioteca de clases, código que usan otros proyectos pero que no se ejecuta solo.
- `mvc`: aplicación web ASP.NET Core MVC, con controladores y vistas de ejemplo.
- `worker`: programa de fondo sin pantalla (Worker Service).
- `xunit`: proyecto de pruebas unitarias.
- `gitignore`: le indica a git que no suba las carpetas `bin/` y `obj/`, que son archivos compilados que se regeneran solos.

## Decisiones tomadas al armar la base

1. **Nombres de las capas en inglés** (`Domain`, `Application`, `Infrastructure`). Los tests de arquitectura del docente reconocen las capas por esos nombres, y es la convención del curso: lo técnico en inglés y lo del negocio en español (`ParqueSolar`, `TareaMantenimiento`).
2. **El simulador es un proyecto aparte** (`src/SolarUY.SensorSimulator`, hoy un submódulo), como dicen `1_clean_architecture.md` y el diagrama de arquitectura. El colector que lo consulta es otro proyecto aparte (`src/SolarUY.Collector`).
3. **El proyecto web se llama `SolarUY.Web`**, como en `1_clean_architecture.md`. Cuando se agreguen los tests de arquitectura del docente (`CodeQuality.Tests`), hay que sumar `.*Web.*` a la constante `PresentationNamespace` de `ArchitectureSetup.cs` para que reconozca esta capa.
4. **Carpetas `src/` y `tests/`** para separar el código de las pruebas.

## Para corregir en `specs.md`

1. **Las entidades van como en el diagrama de clases**, que está más completo: a `specs.md` le faltan `FechaInstalacion` y `Lecturas` en `PanelSolar`, y `PersonalAsignado` en `TareaMantenimiento`.
2. **El simulador es un proyecto aparte**, no una clase dentro de Infrastructure (punto 4.3).
3. **Un controlador por recurso**: `ParquesController`, `NodosController` y `PanelesController`, en lugar de un solo `ParquesController` para todo (punto 5.1). Así cada uno trabaja en su archivo y no hay conflictos en git.
4. **El proyecto web se llama `SolarUY.Web`** (punto 5 dice "SolarUY.Presentation / Web").
5. **Un servicio de aplicación por recurso del ABM**: `ParqueService`, `NodoService` y `PanelService`, en lugar de un único `ParqueService` que además dé de alta nodos y vincule paneles (punto 3.1). Es la misma razón que en los controladores: cada integrante trabaja en su archivo.
6. **Pomelo queda fijado en 9.0.0 y EF Core en 9.0.20**, aunque la solución apunte a `net10.0`. Pomelo todavía no publicó una versión para EF Core 10; la 9.0.0 corre sin problemas sobre .NET 10.
7. **La cultura de la petición se fija en la invariante** (`RequestLocalization` en `Program.cs`). Los formularios HTML mandan siempre los decimales con punto y las fechas en ISO-8601, pero en un equipo configurado para Uruguay (`es-UY`) el valor `"120.5"` se interpretaba como 1205.
8. **El simulador es el mock del hardware, no el que genera las lecturas del sistema.** `specs.md` §4.3 lo describe como un Worker Service que genera datos y los guarda; está implementado al revés y mejor: el simulador emula el sensor por HTTP (caja negra, sin conocer MySQL ni el dominio) y un colector aparte (`src/SolarUY.Collector`) lo consulta, evalúa y guarda. El proyecto que `specs.md` llama "Worker Service" pasó a llamarse `SolarUY.Collector` porque `SolarUY.Simulator` se confundía con `SolarUY.SensorSimulator`.
9. **`Nodo` es el que diagnostica, no el sensor.** §4.3 y la regla 3 de `4_modelado_dominio.md` decían que la caída de potencia se mide contra la potencia nominal del panel y que "el sensor debe emitir" el estado. La regla vigente está en la spec del simulador §9: se mide contra el promedio de los otros paneles activos del nodo, con tres bandas y `DESCONECTADO` para los inactivos, y la calcula `Nodo.EvaluarLecturas` en el dominio (ver `specs.md` §2.1, que además necesita tres campos nuevos).
10. **El sensor ARM es obligatorio en el alta de un nodo** y se carga en el mismo formulario, junto con el inversor. En el dominio sigue siendo opcional (un inversor puede existir antes de que el sensor se conecte), pero el ABM lo exige porque sin dirección el colector no tiene a quién consultarle.

## El ABM de Parques, Nodos y Paneles

Es el primer corte vertical completo del sistema: pantalla → controlador → servicio → repositorio → MySQL.

| Recurso | Controlador | Servicio | Vistas |
|---|---|---|---|
| Parques solares | `ParquesController` | `ParqueService` | `Views/Parques/` |
| Nodos (con su inversor) | `NodosController` | `NodoService` | `Views/Nodos/` |
| Paneles | `PanelesController` | `PanelService` | `Views/Paneles/` |

Cada recurso tiene listado (con filtros), alta, edición, detalle y baja con confirmación. Las pantallas son
vistas Razor comunes sobre ASP.NET Core MVC, no Blazor, como pide `specs.md`.

Reglas de negocio ya implementadas y cubiertas por pruebas:

1. Un nodo no puede existir sin exactamente un inversor (regla 1), por eso el inversor se carga en el mismo formulario del nodo, junto con su sensor ARM.
2. El número de serie del inversor es único en todo el sistema, y el identificador de hardware del sensor también.
3. Un parque con nodos no se puede eliminar, y un nodo con paneles tampoco.
4. El código de panel es único dentro de un nodo: es el vínculo con el panel del sensor.

Las reglas de estado (temperatura y caída de potencia) viven en `Nodo.EvaluarLecturas` y están explicadas en
la sección "Cómo entra la telemetría", con sus tests en `tests/SolarUY.Domain.Tests/Entities/NodoEvaluarLecturasTests.cs`.

La validación de los formularios vive en los view models de `SolarUY.Web/Models` (DataAnnotations). Las reglas
de negocio viven en las entidades de `SolarUY.Domain`, que lanzan `ReglaDeNegocioException` y el controlador
muestra como mensaje de error. Los controladores nunca tocan `Infrastructure`.

## Cómo entra la telemetría (sensor + colector)

El hardware se simula con una **caja negra por HTTP**. El simulador (`SolarUY.SensorSimulator`) emula los
sensores ARM y sus inversores, y expone un contrato HTTP que definimos nosotros
([`SolarUY.SensorSimulator/docs/7_simulador_sensor.md`](SolarUY.SensorSimulator/docs/7_simulador_sensor.md)).
Nuestro sistema lo consulta **por pull**, como si fuera hardware real.

```
SolarUY.Collector  ──GET /sensores/{hw}/estado──▶  SolarUY.SensorSimulator
        │                                                    │
        │ 1. empareja por código de panel                    └─ mide: corriente, voltaje,
        │ 2. Nodo.EvaluarLecturas(...) diagnostica              potencia, temperatura.
        │ 3. guarda en MySQL                                    NO diagnostica.
        ▼
   lecturas_telemetria + paneles.estado
```

| Pieza | Dónde vive | Qué hace |
|---|---|---|
| `ISensorClient` | `SolarUY.Application/Sensors` | El contrato: permite cambiar el simulador por hardware real sin tocar el resto. |
| `SensorHttpClient` | `SolarUY.Infrastructure/Sensors` | La implementación HTTP. Es el único lugar que conoce ese contrato. |
| `ColectorTelemetriaWorker` | `SolarUY.Collector` | Cada `Colector:IntervaloSegundos` consulta cada sensor activo, evalúa y guarda. |
| `Nodo.EvaluarLecturas` | `SolarUY.Domain` | Diagnostica: el sensor mide, el dominio decide. |

**Dónde se ve:** en el detalle de cada panel, la última lectura y la serie de las últimas 20, con las filas
en falla resaltadas. Todavía no hay dashboard ni gráficos: eso es el hito de Monitoreo (`specs.md` §5.2).

**Cómo se vinculan sistema y hardware** (tres campos que hay que hacer coincidir al cargar los datos):

- `SensorDispositivo.IdentificadorHardwareARM` ↔ el identificador del simulador (`ARM-001`, `ARM-002`, …).
- `SensorDispositivo.UrlBase` ↔ dónde escucha (`http://localhost:5100`).
- `PanelSolar.CodigoPanel` ↔ el código del panel en el sensor (`P01`…`P05`), único dentro del nodo.

**Regla de estado** (spec del simulador, sección 9), toda en `Nodo.EvaluarLecturas`:

| Situación | Estado |
|---|---|
| Panel con `Activo = false` | `DESCONECTADO` |
| Temperatura > 75 °C | `FALLA_DETECTADA` |
| Caída de hasta 15 % respecto del promedio de los otros paneles activos del nodo | `OPERATIVO` |
| Caída de más del 15 % y hasta 40 % | `RENDIMIENTO_BAJO` |
| Caída de más del 40 % | `FALLA_DETECTADA` |

La caída se mide contra **el promedio de los paneles hermanos del mismo nodo**, normalizando por potencia
nominal, así que se comparan peras con peras aunque los paneles sean distintos. De noche o con poca luz
(promedio por debajo del 5 % de la nominal) no se evalúa, y con menos de dos paneles activos con lectura no
hay referencia: en esos casos solo manda la temperatura. Está cubierto con tests que prueban las fronteras
del 15 % y del 40 %.

**Demo de una falla:** `POST /simulador/fallas/ARM-001/P01?tipo=sobretemperatura` en el simulador, y en la
siguiente ronda el panel aparece en `FALLA_DETECTADA` en la web.

⚠️ El colector escribe cada pocos segundos: con 3 nodos × 5 paneles son unas 260.000 filas por día. No lo
dejes corriendo días sin una política de retención (spec del simulador, sección 10).

## Cómo trabajamos con git

- Nadie trabaja directo en `main`. Cada uno crea su rama (`git checkout -b nombre-de-la-rama`), sube sus cambios y abre un **pull request** para que otro lo revise antes de mergearlo.
- Antes de empezar a trabajar: `git checkout main` y `git pull`, para tener lo último.

## Próximos pasos

- [x] Entidades del diagrama de clases en `SolarUY.Domain`.
- [x] Conexión a MySQL con Entity Framework Core (`SolarUYDbContext`) en `SolarUY.Infrastructure`.
- [x] ABM de Parques (Guille), Nodos (Fabri) y Paneles (Gonza) — ver la sección "El ABM de Parques, Nodos y Paneles".
- [x] Simulador del sensor (Mariano) incorporado como submódulo, y el colector de telemetría consultándolo y guardando en MySQL.
- [ ] Órdenes al sensor desde la web (activar/desactivar panel y volcado a red). `ISensorClient` ya tiene los métodos; falta la UI.
- [ ] Autenticación con Keycloak y `[Authorize(Roles = "Administrador")]` sobre los tres controladores del ABM.
- [ ] Dashboard de monitoreo en tiempo real (SignalR) y agente MCP con Gemini.
- [ ] Política de retención de `lecturas_telemetria` antes de dejar el colector corriendo varios días.
- [ ] Mover las reglas de IA a `.agents/rules/`.
- [ ] Agregar `CodeQuality.Tests` (tests de arquitectura del docente).
