---
trigger: always_on
---

# Flujo de Trabajo con IA (Desarrollo Asistido y Controlado)

Para cumplir con la rúbrica de evaluación (demostrar control sobre el código generado por IA y evitar código de caja negra), el agente sigue este flujo de manera obligatoria.

## 1. Principios de Desarrollo Asistido por IA

1. **Generación Guiada por Contexto:** Antes de escribir código, el agente tiene en cuenta todas las reglas de esta carpeta y el `specs.md` del requerimiento.
2. **Revisión y Refactorización Manual:** Todo código sugerido por la IA debe ser revisado, compilado y probado por el equipo.
3. **Defensa del Código:** Cada integrante debe ser capaz de explicar línea por línea la lógica del módulo que le fue asignado.

## 2. Desarrollo Guiado por Especificaciones (SDD)

Cada funcionalidad vive en su carpeta `prompting/reqNNN_nombre/` y pasa por tres archivos, en este orden:

1. `requirements.md`: lo escribe una persona del equipo (qué se necesita y criterios de aceptación).
2. `specs.md`: lo genera la skill `sdd-generar-specs` y **lo aprueba una persona** antes de seguir.
3. `tasks.md`: lo genera la skill `sdd-generar-tasks` a partir del `specs.md` aprobado.

Si se pide implementar algo sin `specs.md` aprobado, el agente recuerda este flujo antes de escribir código. Al implementar, sigue el `tasks.md` y marca cada tarea hecha (`[x]`).

## 3. De a una tarea, explicando

* El agente genera **una tarea por vez** y se detiene. No encadena varias tareas ni varias capas en una sola respuesta, aunque se le pida "hacé todo".
* Después de cada bloque de código explica en español, brevemente: qué hace cada clase o método nuevo, por qué va en esa capa, y qué concepto de .NET aparece por primera vez (ej. `async/await`, inyección de dependencias, Tag Helpers).
* Todo método público de `Domain` y `Application` lleva un comentario `/// <summary>` de una o dos líneas. Fuera de eso, comentar solo el **porqué** de algo no evidente.
* Ante dos soluciones válidas, elige la más simple de leer. No agrega patrones, librerías ni abstracciones que el requerimiento no necesita.
* Si el usuario no entiende algo, lo explica (o lo simplifica) antes de seguir.

## 4. Auto-Validación con los Tests de Arquitectura

* Cada vez que el agente crea o modifica código, ejecuta `dotnet build` y luego `dotnet test` sobre el proyecto `CodeQuality.Tests` (ArchUnitNET).
* Si un test falla, **no** le pide al usuario que lo arregle ni da la tarea por terminada: lee el error, corrige su código y vuelve a ejecutar. Solo reporta cuando todo está en verde.

## 5. Pruebas Unitarias del Dominio

* Cada vez que se genera o modifica lógica en `Domain` (métodos de entidades, validaciones, cálculos), el agente genera la prueba unitaria xUnit correspondiente.
* En `Application`, `Infrastructure` y `Web` no genera pruebas unitarias salvo que el usuario lo pida.

## 6. Prohibido Alterar los Tests de Arquitectura

* El agente tiene **prohibido** modificar los archivos de `CodeQuality.Tests` para que un test pase. Si un test falla, se corrige el código, nunca el test.
* Si cree que una regla está mal planteada o que se justifica una excepción, se **detiene**, lo explica y pide autorización explícita antes de tocar cualquier archivo de esos tests.

## 7. Alcance de los Cambios

* Modificar solo los archivos que la tarea necesita. No reformatear ni "mejorar" código ajeno: cada integrante trabaja en su módulo.
* No agregar paquetes NuGet sin avisar qué son y esperar confirmación.
* Nunca escribir secretos (contraseñas, API key de Gemini, cadenas de conexión, secreto de cliente de Keycloak) en archivos versionados: van en *user secrets* en desarrollo y en variables de entorno en el servidor.
* No ejecutar `dotnet ef database update` ni borrar migraciones sin que se pida.

## 8. Integración de MCP (Model Context Protocol)

* El proyecto expone herramientas MCP para que Gemini consulte datos del sistema:
  * `ObtenerResumenParqueTool(parqueId)`
  * `ObtenerGeneracionHoyTool()`
  * `ObtenerGeneracionHistoricaTool(parqueId, desde, hasta, agrupacion)`
  * `ListarPanelesEnFallaTool()`
* **Todas las herramientas son de solo lectura.** El agente nunca genera una herramienta que cree, modifique o borre datos, ni que envíe órdenes al inversor: la letra exige que el lenguaje natural sea solo para consultar.
* Flujo: el chat web envía la pregunta del usuario a Gemini junto con la descripción de las herramientas; Gemini indica qué herramienta necesita; la aplicación la ejecuta a través del servidor MCP y le devuelve el resultado; Gemini redacta la respuesta en lenguaje natural.

## 9. Registro del Uso de IA

Al terminar un requerimiento, se agrega al final de su `tasks.md` una sección `## Uso de IA`: qué generó el agente, qué corrigió o reescribió la persona, y qué problemas detectaron los tests o la revisión.
