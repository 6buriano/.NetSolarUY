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
│   ├── SolarUY.Application/      ← servicios (casos de uso) y DTOs
│   ├── SolarUY.Infrastructure/   ← base de datos (EF Core + MySQL), repositorios, Keycloak, MCP
│   ├── SolarUY.Web/              ← aplicación MVC: controladores y vistas
│   └── SolarUY.Simulator/        ← simulador del inversor (Worker Service)
├── tests/
│   └── SolarUY.Domain.Tests/     ← pruebas unitarias del dominio (xUnit)
└── *.md                          ← reglas de desarrollo con IA y specs
```

`SolarUY.slnx` es el formato de solución de .NET 10 (antes era `.sln`).

## Qué va en cada proyecto

| Proyecto | Tipo | Qué contiene | Ejemplo |
|---|---|---|---|
| `SolarUY.Domain` | Biblioteca de clases | Las entidades del diagrama de clases y sus reglas. Es C# puro, sin librerías externas. | `ParqueSolar`, `EstadoSalud` |
| `SolarUY.Application` | Biblioteca de clases | Los casos de uso: lo que el sistema hace. Usa interfaces para no depender de la base de datos. | `ParqueService`, `IParqueRepository` |
| `SolarUY.Infrastructure` | Biblioteca de clases | Los detalles técnicos: cómo se guarda en MySQL, cómo se habla con Keycloak o con Gemini. | `SolarUYDbContext`, `ParqueRepository` |
| `SolarUY.Web` | Aplicación MVC | Las pantallas. Recibe pedidos del navegador y llama a los servicios. | `ParquesController`, `Views/Parques/Index.cshtml` |
| `SolarUY.Simulator` | Worker Service | Programa sin pantalla que corre de fondo y genera lecturas falsas de los paneles. | `Worker.cs` |
| `SolarUY.Domain.Tests` | Proyecto xUnit | Pruebas de la lógica del dominio. | `ParqueSolarTests` |

## Quién puede usar a quién (referencias)

Cada proyecto solo puede usar el código de los proyectos que tiene como referencia. Así se cumple Clean Architecture: las dependencias apuntan siempre hacia el centro (Domain).

| Proyecto | Referencia a | Por qué |
|---|---|---|
| Domain | nada | Es el núcleo del negocio y no depende de nadie |
| Application | Domain | Los casos de uso trabajan con las entidades |
| Infrastructure | Application, Domain | Implementa las interfaces que pide Application (por ejemplo, guardar en MySQL) |
| Web | Application, Domain, Infrastructure | Es el punto de arranque de la web |
| Simulator | Application, Domain, Infrastructure | Es el punto de arranque del simulador |
| Domain.Tests | Domain | Prueba las entidades |

⚠️ **Web referencia a Infrastructure solo para `Program.cs`**, que es donde se conectan las interfaces con sus implementaciones (inyección de dependencias). **Los controladores nunca usan Infrastructure directamente** (ni el `DbContext` ni los repositorios concretos): siempre llaman a un servicio de Application. Lo mismo vale para el Simulator.

## Cómo compilar y correr

Desde la carpeta raíz del repo:

```bash
dotnet build                               # compila toda la solución
dotnet run --project src/SolarUY.Web       # levanta la web (abrir la URL que muestra)
dotnet run --project src/SolarUY.Simulator # levanta el simulador
dotnet test                                # corre las pruebas
```

Para cortar un programa que está corriendo: `Ctrl + C` en la terminal.

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
dotnet new worker -n SolarUY.Simulator -o src/SolarUY.Simulator
dotnet new xunit -n SolarUY.Domain.Tests -o tests/SolarUY.Domain.Tests

# 4. Agregar los proyectos a la solución y crear el .gitignore
dotnet sln add src/*/*.csproj tests/*/*.csproj
dotnet new gitignore

# 5. Referencias entre proyectos (ver tabla de arriba)
dotnet add src/SolarUY.Application/SolarUY.Application.csproj reference src/SolarUY.Domain/SolarUY.Domain.csproj
dotnet add src/SolarUY.Infrastructure/SolarUY.Infrastructure.csproj reference src/SolarUY.Application/SolarUY.Application.csproj src/SolarUY.Domain/SolarUY.Domain.csproj
dotnet add src/SolarUY.Web/SolarUY.Web.csproj reference src/SolarUY.Application/SolarUY.Application.csproj src/SolarUY.Domain/SolarUY.Domain.csproj src/SolarUY.Infrastructure/SolarUY.Infrastructure.csproj
dotnet add src/SolarUY.Simulator/SolarUY.Simulator.csproj reference src/SolarUY.Application/SolarUY.Application.csproj src/SolarUY.Domain/SolarUY.Domain.csproj src/SolarUY.Infrastructure/SolarUY.Infrastructure.csproj
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
2. **El simulador es un proyecto aparte** (`SolarUY.Simulator`), como dicen `1_clean_architecture.md` y el diagrama de arquitectura.
3. **El proyecto web se llama `SolarUY.Web`**, como en `1_clean_architecture.md`. Cuando se agreguen los tests de arquitectura del docente (`CodeQuality.Tests`), hay que sumar `.*Web.*` a la constante `PresentationNamespace` de `ArchitectureSetup.cs` para que reconozca esta capa.
4. **Carpetas `src/` y `tests/`** para separar el código de las pruebas.

## Para corregir en `specs.md`

1. **Las entidades van como en el diagrama de clases**, que está más completo: a `specs.md` le faltan `FechaInstalacion` y `Lecturas` en `PanelSolar`, y `PersonalAsignado` en `TareaMantenimiento`.
2. **El simulador es un proyecto aparte** (`SolarUY.Simulator`), no una clase dentro de Infrastructure (punto 4.3).
3. **Un controlador por recurso**: `ParquesController`, `NodosController` y `PanelesController`, en lugar de un solo `ParquesController` para todo (punto 5.1). Así cada uno trabaja en su archivo y no hay conflictos en git.
4. **El proyecto web se llama `SolarUY.Web`** (punto 5 dice "SolarUY.Presentation / Web").

## Cómo trabajamos con git

- Nadie trabaja directo en `main`. Cada uno crea su rama (`git checkout -b nombre-de-la-rama`), sube sus cambios y abre un **pull request** para que otro lo revise antes de mergearlo.
- Antes de empezar a trabajar: `git checkout main` y `git pull`, para tener lo último.

## Próximos pasos

- [ ] Entidades del diagrama de clases en `SolarUY.Domain`.
- [ ] Conexión a MySQL con Entity Framework Core (`SolarUYDbContext`) en `SolarUY.Infrastructure`.
- [ ] Mover las reglas de IA a `.agents/rules/`.
- [ ] Agregar `CodeQuality.Tests` (tests de arquitectura del docente).
- [ ] ABM de Parques (Guille), Nodos (Fabri) y Paneles (Gonza).
- [ ] Simulador (Mariano).
