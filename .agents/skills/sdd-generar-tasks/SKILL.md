---
name: sdd-generar-tasks
description: Genera la lista de tareas de implementación (tasks.md) a partir de un specs.md ya aprobado de una funcionalidad de SolarUY
---

# Instrucciones

**Rol:** Asume el rol de un **Líder Técnico** que divide un diseño en pasos pequeños para que un estudiante pueda implementarlos y entenderlos de a uno.

Cuando el usuario invoque esta skill, debes ejecutar el siguiente flujo de trabajo de manera estricta:

1. **Localizar el Diseño:** Busca el archivo `specs.md` dentro de la carpeta indicada por el usuario (dentro de `prompting/`). Si no existe, o si todavía tiene una sección "Preguntas abiertas" sin resolver, **detente** y avísale al usuario: no se planifica sobre un diseño sin aprobar.
2. **Generar Tareas:** Redacta el archivo `tasks.md` en esa misma carpeta, como una lista de casillas (`- [ ]`) numeradas.
3. **Orden de las Tareas:** Agrupa las tareas por capa y respeta este orden, que va de adentro hacia afuera para que cada paso compile sobre el anterior:
   1. Capa Domain (incluye la prueba unitaria de cada comportamiento de dominio).
   2. Capa Infrastructure (configuración EF, repositorio, migración).
   3. Capa Application (DTOs, interfaz y servicio).
   4. Registro de dependencias en `Program.cs`.
   5. Capa Presentation (ViewModels, controlador, vistas, enlace en el menú).
   6. Verificación de Calidad.
4. **Tamaño de las Tareas:** Cada tarea debe poder hacerse y revisarse en pocos minutos: una clase, un método o una vista por tarea. Nombra el archivo o la clase exacta que se crea o modifica.
5. **Verificación de Calidad:** La última sección es siempre la misma:
   - Compilar la solución (`dotnet build`).
   - Ejecutar los tests de arquitectura (`dotnet test` sobre `CodeQuality.Tests`).
   - Ejecutar los tests unitarios del dominio.
   - Probar a mano cada criterio de aceptación del `requirements.md`, listando uno por casilla.
   - Completar la sección `## Uso de IA` (ver regla `6_flujo_trabajo_ia.md`, sección 9).
6. **Fidelidad al Diseño:** No agregues tareas que no surjan del `specs.md`. Si al planificar detectas que al diseño le falta algo, avísale al usuario en lugar de agregarlo por tu cuenta.

Al finalizar, escribe el archivo `tasks.md` en el disco y avísale al usuario que ya puede empezar a implementar, **una tarea por vez**. No escribas código en esta misma ejecución.
