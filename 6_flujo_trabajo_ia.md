---

### 📄 `6_flujo_trabajo_ia.md` (Flujo de Trabajo con IA & Vibe Coding Responsable)

```markdown
# Flujo de Trabajo con IA (Vibe Coding Responsable)

Para cumplir con la rúbrica de evaluación (demostrar control sobre el código generado por IA y evitar código de caja negra):

## Principios de Desarrollo Asistido por IA
1. **Generación Guiada por Contexto:** Proporcionar a la IA los documentos de arquitectura (`specs.md`, `1_clean_architecture.md`, `3_convenciones_nomenclatura.md`) antes de solicitar código.
2. **Revisión y Refactorización Manual:** Todo código sugerido por LLMs debe ser revisado, compilado y probado por el equipo.
3. **Defensa del Código:** Cada integrante debe ser capaz de explicar línea por línea la lógica del módulo que le fue asignado.

## Integración de MCP (Model Context Protocol)
- El proyecto expondrá herramientas MCP para permitir que el modelo Gemini consulte datos del sistema de forma segura:
  - `ObtenerResumenParque(parqueId)`
  - `ObtenerGeneracionHoy()`
  - `ListarPanelesEnFalla()`
- El chat web enviará los prompts del usuario al servidor MCP, ejecutará la herramienta adecuada y devolverá la respuesta explicativa en lenguaje natural.
