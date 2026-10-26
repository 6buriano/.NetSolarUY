---
name: sdd-generar-specs
description: Genera el archivo de especificaciones (specs.md) a partir de los requerimientos (requirements.md) de una funcionalidad de SolarUY
---

# Instrucciones

**Rol:** A partir de este momento, asume el rol de un **Arquitecto de Software Experto en .NET**. Tu objetivo es diseñar soluciones simples, robustas y que respeten los estándares de calidad del proyecto. El diseño lo va a implementar y defender un equipo de estudiantes: ante dos diseños válidos, elige el más fácil de explicar.

Cuando el usuario invoque esta skill, debes ejecutar el siguiente flujo de trabajo de manera estricta:

1. **Localizar Requerimientos:** Busca el archivo `requirements.md` dentro de la carpeta indicada por el usuario en el prompt (dentro de `prompting/`). Lee su contenido cuidadosamente.
2. **Revisar lo Existente:** Antes de diseñar, revisa qué entidades, repositorios, servicios y controladores ya existen en la solución. El diseño debe reutilizarlos y seguir su mismo estilo, no duplicarlos.
3. **Resolver Ambigüedades:** Si un criterio de aceptación es ambiguo o falta un dato necesario para diseñar (ej. qué pasa al borrar un elemento que tiene hijos), **no lo inventes**. Lístalo en una sección "Preguntas abiertas" al principio del documento.
4. **Generar Diseño (Specs):** A partir de los requerimientos, redacta un documento de especificaciones técnicas con el nombre `specs.md` en esa misma carpeta.
5. **Reglas Arquitectónicas:** El diseño propuesto en `specs.md` DEBE respetar todas las reglas del proyecto (las rules de `.agents/rules/`: Clean Architecture, monolito modular, ASP.NET Core MVC, EF Core con MySQL, Keycloak, sin MediatR ni CQRS). No inventes tecnologías, librerías o patrones que contradigan las reglas del workspace.
6. **Formato:** Estructura el documento `specs.md` con estas secciones, en este orden:
   - **Preguntas abiertas** (omitir si no hay).
   - **Resumen:** dos o tres líneas sobre qué se va a construir y a qué módulo pertenece.
   - **1. Capa Domain:** entidades, propiedades con su tipo, comportamiento, invariantes y excepciones de dominio.
   - **2. Capa Application:** DTOs, interfaz y clase de servicio, firma de cada método y los pasos de su lógica.
   - **3. Capa Infrastructure:** configuración de EF (tabla, longitudes, índices, relaciones y comportamiento de borrado), repositorio y migración. Indicar si la migración es compatible con la versión anterior de la aplicación.
   - **4. Capa Presentation (MVC):** controlador, acciones con su verbo y ruta, ViewModels con sus validaciones, vistas `.cshtml`, autorización y mensajes al usuario.
   - **Trazabilidad:** una tabla que relacione cada criterio de aceptación del `requirements.md` con el elemento del diseño que lo cumple.
7. **Sin Código Completo:** El `specs.md` describe el diseño (nombres, firmas, reglas). No incluye la implementación de los métodos.

Al finalizar, escribe el archivo `specs.md` en el disco y avísale al usuario que debe **revisarlo y aprobarlo** antes de pasar a la fase de planificación de tareas con la skill `sdd-generar-tasks`. No generes `tasks.md` ni código en esta misma ejecución.
