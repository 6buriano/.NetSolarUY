# Guion del video de SolarUY

Material para mostrar la solución a los demás compañeros del curso: qué hace el sistema y cómo se hizo.

| | |
|---|---|
| **Video** | `Documentacion/video/demo-solaruy.mp4` — 3:20, 1400×900, sin voz, con rótulos |
| **Presentación** | `Documentacion/PRESENTACION-SolarUY.pptx` — 10 diapositivas, con el guion en las notas |
| **Cómo se genera** | `.entrega-build/generar-presentacion.py` y el script de grabación (ver el final) |
| **Estado** | Grabado contra el sistema desplegado, con datos del parque de prueba |

## Cómo usarlo

1. **Tal cual**: el video se reproduce solo; los rótulos explican qué se está viendo. Sirve para
   proyectar en clase o subirlo al aula virtual.
2. **Con voz**: alguien narra el texto de la columna «Narración» mientras pasa el video (o sobre las
   diapositivas). El video está pensado para eso: los bloques duran lo que el texto.
3. **En vivo**: las diapositivas son el esqueleto de una presentación de 3 a 4 minutos; el video queda
   como respaldo.

## Recorrido del video

Los tiempos son aproximados: el video completo dura 3:20.

| Momento | Qué se ve | Narración sugerida |
|---|---|---|
| 0:00 | Monitoreo, la página de inicio | «Esta es la pantalla de inicio. Muestra cuánto genera el parque, cada nodo y cada panel, con su estado. Se actualiza sola cada cinco segundos: nadie tiene que recargar.» |
| 0:09 | Parques | «El sistema administra parques solares con un ABM completo: listado con búsqueda, alta, edición, detalle y baja.» |
| 0:15 | Nodos | «Cada nodo tiene exactamente un inversor y, opcionalmente, un sensor ARM, que es el que mide.» |
| 0:21 | Alta de nodo | «Si al dar de alta el identificador del sensor no existe todavía, la pantalla no nos deja con un nodo muerto: avisa cuáles existen y ofrece crearlo o cargar el nodo sin sensor.» |
| 0:28 | Paneles | «Los paneles tienen estado de salud y filtros; desde acá se entra al detalle.» |
| 0:34 | Detalle del panel | «La última lectura, las últimas veinte y el historial de mantenimiento. Las lecturas nuevas aparecen arriba solas, sin recargar: la pantalla está suscrita al monitoreo.» |
| 0:46 | Sobretemperatura forzada | «Forzamos una sobretemperatura en el sensor. El colector la lee, el dominio la diagnostica como falla y la pantalla se actualiza sola. La regla no está en la pantalla: está en el dominio, y tiene sus tests.» |
| 0:57 | Vuelve a operativo | «Cuando la temperatura se normaliza, el panel vuelve a operativo en la ronda siguiente.» |
| 1:03 | Mantenimiento | «Cada panel guarda su historial de tareas. Programamos una: panel, tipo, fecha, descripción y responsable.» |
| 1:17 | Iniciar | «La tarea nace pendiente y se inicia cuando el técnico empieza.» |
| 1:20 | Completar con fecha y observaciones | «Al completarla se registran la fecha y hora reales del trabajo —las informa el técnico, porque la del servidor no coincide si la marca más tarde— y una observación con lo que se hizo. Esto salió de la devolución del docente.» |
| 1:35 | Histórico | «El histórico agrupa la generación por día, mes o año, con potencia promedio, máxima y energía estimada. Es estimada porque se calcula con la potencia de cada lectura; sirve para comparar, no para facturar.» |
| 1:46 | Consultas en lenguaje natural | «Y se puede preguntar en castellano. El agente no inventa: consulta la base con cinco herramientas de solo lectura que expone un servidor MCP aparte, y responde con los datos del parque.» |
| 2:11 | Ayuda | «Las explicaciones de uso viven en una pantalla de Ayuda, no repetidas en cada pantalla. Eso también salió de la devolución.» |
| 2:17 | Cierre | «En resumen: .NET 10, arquitectura en capas, MySQL, Keycloak, SignalR y MCP, con 216 pruebas automáticas y verificación de arquitectura en cada push.» |

## Qué contar del «cómo se hizo» (diapositivas 8 y 9)

- **Especificaciones antes que código**: cada funcionalidad tiene su carpeta en `prompting/` con
  `requirements.md` (criterios de aceptación), `specs.md` (diseño capa por capa) y `tasks.md` (pasos,
  con una sección «Uso de IA» al final).
- **Una tarea por vez, un commit por tarea**: cada paso se compila y se prueba antes de commitear.
- **Verificación automática**: 216 pruebas (115 de dominio, 20 de aplicación, 11 de arquitectura con
  ArchUnitNET y 70 del simulador), 0 advertencias de compilación, análisis con SonarQube e integración
  continua en cada push y pull request.
- **La devolución del docente como insumo**: de ahí salieron la pantalla de Ayuda (menos texto en las
  pantallas), el cierre de mantenimiento con fecha real y observaciones, y el criterio de escribir para
  quien opera el parque y no para un programador.

## Cómo volver a grabar el video

1. Necesita el sitio arriba y una cuenta con rol `Administrador`.
2. El script entra sin grabar, guarda la sesión y recién ahí empieza la grabación (así el login con la
   clave no queda en el video). Recorre las pantallas, fuerza una falla en el simulador y la levanta al
   final, incluso si algo falla.
3. Los rótulos y el cursor se dibujan dentro de la página, así que el video sale con las explicaciones
   quemadas.
4. Después se convierte a MP4 con `ffmpeg`.

Detalles que conviene cuidar al regrabar:

- **Horario**: durante el día la generación es distinta de cero y las capturas se ven mejor; de noche
  todos los paneles marcan 0 W.
- **Cuota de Gemini**: la consulta en lenguaje natural consume uno de los ~20 pedidos diarios de la capa
  gratuita. Conviene ensayar antes y dejar una sola consulta para la toma final.
- **La falla forzada** se hace sobre el panel `P01` del sensor `ARM-001` del parque de prueba, y se
  levanta con `DELETE /simulador/fallas/ARM-001/P01`.
- **La tarea de mantenimiento** que se crea queda en el sistema con la observación del cierre; si
  molestan las tareas de prueba, se pueden cancelar desde la pantalla.

## Material relacionado

- Documento completo del sistema: `Documentacion/DOCUMENTACION-SolarUY.md` (y su `.docx`).
- Capturas de todas las pantallas: `entrega-1-capturas/`.
- Los dos diagramas: `entrega-1-diagramas/` (arquitectura y clases).
