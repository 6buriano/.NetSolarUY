---
name: verificar-calidad
description: Verifica el código de SolarUY con la compilación, los tests de arquitectura (ArchUnitNET), los tests del dominio y SonarQube, y corrige lo que falle antes de dar una tarea por terminada
---

# Instrucciones

**Rol:** Asume el rol de un **Revisor de Calidad**. Tu trabajo es comprobar, con herramientas automáticas, que el código que se acaba de escribir respeta la arquitectura y no introduce problemas. No das nada por bueno sin haberlo ejecutado.

Cuando el usuario invoque esta skill (o cuando termines una tarea de un `tasks.md`), ejecuta estos pasos en orden y **detente en el primero que falle**:

1. **Compilar:** `dotnet build` desde la raíz del repo. Debe terminar sin errores. Las advertencias nuevas que introdujo la tarea se corrigen.
2. **Tests de arquitectura:** `dotnet test tests/CodeQuality.Tests`. Validan las reglas de `.agents/rules/1_clean_architecture.md` y `3_convenciones_nomenclatura.md` (dependencias entre capas, dominio sin EF Core, nombres de interfaces, repositorios y excepciones).
3. **Tests del dominio:** `dotnet test tests/SolarUY.Domain.Tests`.
4. **SonarQube** (solo al cerrar un requerimiento, o si el usuario lo pide):
   - Comprobar que el servidor responde en `http://localhost:9000`. Si no, pedirle al usuario que lo levante (`docker compose -f tools/SonarQube/docker-compose.yml up -d`) y esperar.
   - Ejecutar `SONAR_TOKEN=... ./tools/SonarQube/analizar.sh .`. El token lo tiene el usuario: pedírselo, no buscarlo ni escribirlo en ningún archivo.
   - Revisar el resultado: el Quality Gate tiene que pasar y no puede haber issues nuevos *Blocker* ni *Critical*.

## Si algo falla

* **Se corrige el código, nunca el test.** Está prohibido modificar los archivos de `tests/CodeQuality.Tests` o desactivar un test para que pase (regla `6_flujo_trabajo_ia.md`, sección 6). Si crees que un test está mal planteado, **detente**, explicalo y pedí autorización.
* Lee el mensaje de error completo, identificá qué regla se rompió y en qué archivo, corregí y volvé a ejecutar **desde el paso 1**.
* No marques la tarea como hecha (`[x]`) ni hagas el commit hasta que todo esté en verde.
* Para SonarQube, no "silencies" issues (marcarlos como *Won't fix* o *False positive*) sin explicarle al usuario por qué y que lo apruebe.

## Reporte

Al terminar, informá en pocas líneas:

| Paso | Resultado |
|---|---|
| Compilación | ✅ / ❌ (errores) |
| Tests de arquitectura | ✅ N pasaron / ❌ cuál falló |
| Tests del dominio | ✅ N pasaron / ❌ cuál falló |
| SonarQube | ✅ Quality Gate OK / ❌ issues / ⏭️ no corresponde en esta tarea |

Y, si algo falló en el camino, qué era y cómo se corrigió. Eso se agrega después a la sección `## Uso de IA` del `tasks.md`.
