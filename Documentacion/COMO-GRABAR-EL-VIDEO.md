# Cómo grabar el video de SolarUY

Instructivo para grabar la demostración con voz propia, sin depender de nadie más. Está pensado para
una toma de **3 a 4 minutos** con el sistema funcionando.

Si preferís no grabar, ya existe un video mudo con rótulos (`Documentacion/video/demo-solaruy.mp4`) y
las diapositivas (`Documentacion/PRESENTACION-SolarUY.pptx`). Esto es para hacerlo con narración.

## 0. Antes de apretar «grabar»

| Chequeo | Por qué |
|---|---|
| **Grabar de día** | De noche todos los paneles marcan 0 W y la demo pierde gracia |
| **Entrar al sistema justo antes de grabar** | La sesión de Keycloak caduca por inactividad (≈30 min) y te manda al login en medio de la toma |
| **Navegador limpio**: ventana 1920×1080, zoom 100–110 %, sin barra de marcadores, sin extensiones | El texto tiene que leerse en el video |
| **Notificaciones apagadas**: `Win + A` → **No molestar** | Un cartel de Windows en medio de la demo arruina la toma |
| **Cerrar correo, Teams y pestañas ajenas** | Nada de datos personales en pantalla |
| **Una tarea de mantenimiento `PENDIENTE`** (o crear una en el video) | Es el bloque del cierre con fecha y observaciones |
| **La consulta a Gemini ensayada** | La capa gratuita son ~20 pedidos por día: no la gastes en pruebas |
| **Micrófono probado** (10 segundos de prueba y escuchar) | El audio se perdona menos que la imagen |

**Sobre el correo del usuario:** la barra muestra «Hola, <usuario>». Si en la cuenta de administrador el
usuario es un correo, en el video se va a ver. Tres salidas: recortar el borde superior en la edición,
crear un usuario de Keycloak con nombre corto (`admin`), o dejar el borde visible (no es un dato
sensible, pero queda más prolijo sin él).

## 1. Con qué grabar

| Herramienta | Cuándo conviene |
|---|---|
| **Xbox Game Bar** (`Win + G` → botón de grabar) | Sin instalar nada. Graba la ventana activa y guarda un MP4 en `Videos\Capturas`. Alcanza para la demo. |
| **OBS Studio** | Más control: escena fija, 1080p, 30 fps, micrófono separado del audio del sistema. Es el que da mejor resultado. |
| **PowerPoint → Grabar presentación** | Si van a mostrar las diapositivas con voz: graba la narración por diapositiva y exporta un MP4. |

Configuración recomendada: **1920×1080, 30 fps, cursor visible, micrófono del headset**. Grabar a
1080p y no a 720p: el texto de las tablas se nota.

## 2. El guion, bloque por bloque

Total: unos **3 minutos 30**. Los tiempos son para la toma, no para la edición. La idea es una sola
por bloque: **no narres lo que se ve, contá por qué importa**.

| # | Bloque | Tiempo | Qué hacer | Qué decir (guía, no leer) |
|---|---|---|---|---|
| 1 | **Apertura** | 15 s | Pantalla de Monitoreo quieta | «Somos el equipo 6. SolarUY administra parques solares y sigue la generación de cada panel a partir de lo que miden los sensores. Esto es la pantalla de inicio: se actualiza sola cada cinco segundos.» |
| 2 | **Monitoreo en vivo** | 15 s | Esperar a que cambie la hora de «Actualizado» | «No hay que recargar: el servidor le empuja los datos al navegador. La generación del parque, de cada nodo y de cada panel, con su estado.» |
| 3 | **ABM** | 20 s | Menú **Parques** → **Nodos** → abrir un nodo | «El ABM es completo: listado con búsqueda, alta, edición, detalle y baja. Cada nodo tiene un inversor y, opcionalmente, un sensor ARM, que es el que mide.» |
| 4 | **Alta con sensor inexistente** | 20 s | **Nodos → Nuevo nodo**, escribir `ARM-999` y guardar | «Si el identificador no existe, no nos deja con un nodo muerto: avisa cuáles existen y ofrece crearlo o cargar el nodo sin sensor.» *(No hace falta completar todo el formulario: sin el resto de los datos no crea nada.)* |
| 5 | **Panel con lecturas en vivo** | 20 s | **Paneles → Ver** en `P01` (JAM) | «La última lectura y las últimas veinte, en hora de Montevideo. Las nuevas aparecen arriba solas: la pantalla está suscrita al monitoreo.» |
| 6 | **Falla diagnosticada** | 25 s | Forzar la falla (ver §3) y quedarse en esa pantalla | «Forzamos una sobretemperatura en el sensor. El colector la lee, el dominio la diagnostica como falla y la pantalla se actualiza sola. La regla está en el dominio, con sus tests, no en la pantalla.» |
| 7 | **Mantenimiento** | 30 s | **Mantenimiento → Ver** en una tarea pendiente → **Iniciar** → completar fecha y observaciones → **Completar** | «La tarea nace pendiente y se inicia cuando el técnico empieza. Al completarla se registran la fecha real del trabajo y una observación de lo que se hizo: esto salió de la devolución del docente.» |
| 8 | **Histórico** | 15 s | **Histórico** → elegir panel y **Consultar** | «La generación por día, mes o año, con promedio, máximo y energía estimada. Estimada porque se calcula con la potencia de cada lectura: sirve para comparar, no para facturar.» |
| 9 | **Consultas en lenguaje natural** | 25 s | **Consultas** → escribir la pregunta → **Enviar** | «Se puede preguntar en castellano. El agente no inventa: consulta la base con cinco herramientas de solo lectura que expone un servidor MCP aparte.» |
| 10 | **Ayuda** | 10 s | Menú **Ayuda** | «Las explicaciones viven acá, no repetidas en cada pantalla.» |
| 11 | **Cierre** | 15 s | Volver a Monitoreo | «En resumen: .NET 10 en capas, MySQL, Keycloak, SignalR y MCP, con 216 pruebas automáticas y verificación de arquitectura en cada push.» |

**Regla de oro:** si un bloque sale mal, no cortes la toma: dejá dos segundos de silencio y repetí el
bloque. Después se corta en la edición y queda una toma limpia.

## 3. Cómo forzar fallas y errores

Todo se hace contra el simulador, que es el que hace de hardware. Los comandos sirven desde la terminal
de Windows (PowerShell) o desde el navegador. **Ojo: el simulador de producción es público, así que
cualquiera puede forzar una falla; no lo dejes forzado al terminar.**

```powershell
# Sobretemperatura en el panel P01 del sensor ARM-001 (la falla "linda" para el video)
Invoke-RestMethod -Method Post "http://simulador.fabriq.uy/simulador/fallas/ARM-001/P01?tipo=sobretemperatura"

# Caída de potencia (el panel produce mucho menos que los otros del nodo)
Invoke-RestMethod -Method Post "http://simulador.fabriq.uy/simulador/fallas/ARM-001/P01?tipo=caidaPotencia"

# Levantar la falla y dejar el panel normal
Invoke-RestMethod -Method Delete "http://simulador.fabriq.uy/simulador/fallas/ARM-001/P01"
```

Con `curl` es lo mismo:

```bash
curl -X POST "http://simulador.fabriq.uy/simulador/fallas/ARM-001/P01?tipo=sobretemperatura"
curl -X DELETE "http://simulador.fabriq.uy/simulador/fallas/ARM-001/P01"
```

Qué esperar:

- La respuesta es **200** al forzar y **204** al levantar. Un tipo que no existe devuelve **400**.
- La pantalla tarda en reflejarlo: el colector consulta cada 5 segundos, así que **esperá 15 o 20
  segundos** antes de seguir hablando.
- Para saber qué está forzado sin adivinar: `curl http://simulador.fabriq.uy/simulador/inventario` y
  buscá `"falla"` y `"fallaForzada"` en el panel.
- Si la falla no aparece en la pantalla, revisá que el colector esté corriendo (`docker ps` en el
  servidor) y que la sesión no se haya vencido.

**Otros errores que se pueden mostrar** (sin tocar nada):

| Qué mostrar | Cómo | Qué se ve |
|---|---|---|
| Alta con un sensor que no existe | **Nodos → Nuevo nodo**, `ARM-999`, guardar | Aviso con los que existen y las dos salidas |
| Validación del cierre | **Mantenimiento → Iniciar → Completar** con una fecha futura | «La fecha de realización no puede ser posterior al momento actual» |
| Validación de formulario | Guardar un alta con campos vacíos | Mensajes en rojo por campo |
| Sin permisos | Entrar con un usuario de rol `Consulta` y tocar Parques | «No tenés permiso» (Monitoreo sí lo ve) |
| El MCP caído (sólo en local) | `docker compose stop mcp` y hacer una consulta | El aviso de falla en Consultas; el resto del sistema sigue andando |

## 4. Cómo grabar

1. **Probar el audio**: grabá 10 segundos, escuchá y ajustá el volumen. Un audio bajo no se arregla
   después.
2. **Grabar por bloques de dos o tres**, con dos segundos de silencio entre uno y otro. Es mucho más
   fácil que intentar una toma perfecta de tres minutos.
3. **Hablá más lento de lo que te parece natural**, y no leas: el guion es una guía, no un texto.
4. **Abrí con el problema, no con la tecnología**: «administrar un parque solar a mano es un lío» pega
   más que «usamos .NET 10».
5. **Cerrá con el número**: 216 pruebas, dos réplicas, cero advertencias. Los números se recuerdan.

## 5. Edición mínima

- Unir los bloques, recortar el primer y el último segundo de cada uno.
- Subir el volumen si quedó bajo; si hay ruido de fondo, un filtro de reducción de ruido alcanza.
- Exportar **MP4 H.264, 1080p, 30 fps**. El nombre: `SolarUY-equipo6-demo.mp4`.
- Los subtítulos los genera YouTube solo, o se pueden escribir desde la columna «Qué decir».

## 6. Dónde publicarlo

- **Aula virtual**: subir el MP4 directo (3–4 minutos pesan poco, ~50–100 MB).
- **YouTube (no listado)**: la descripción puede llevar el repositorio, `github.com/6buriano/.NetSolarUY`,
  rama `main`.
- **Drive**: útil si el aula virtual tiene límite de tamaño.

## 7. Problemas típicos

| Síntoma | Causa | Solución |
|---|---|---|
| Todos los paneles marcan **0 W** | Es de noche | Grabar de día |
| La pantalla dice **«Conectando…»** | Se cortó SignalR | Recargar la página; si sigue, revisar la réplica |
| **Te manda al login** en medio de la toma | La sesión de Keycloak caducó | Volver a entrar y cortar esa parte en la edición |
| La falla **no aparece** | El colector no corrió todavía | Esperar 15–20 s o verificar con el inventario |
| La consulta contesta con **error** | Cuota de Gemini agotada (429) | Esperar al día siguiente o cambiar de modelo |
| El texto se ve **chico** | Grabación a 720p o zoom bajo | Grabar 1080p y usar zoom 110–125 % en el navegador |
| Aparece el **correo del usuario** | La cuenta es un correo | Recortar en la edición o usar un usuario con nombre corto |

## 8. Si quieren mostrar también el «cómo se hizo»

Son 40 segundos más, y conviene hacerlo con las diapositivas 8 y 9 del
`Documentacion/PRESENTACION-SolarUY.pptx` (o mostrando el repositorio):

- **Especificaciones antes que código**: cada funcionalidad tiene su carpeta en `prompting/` con
  `requirements.md`, `specs.md` y `tasks.md`.
- **Una tarea por vez, un commit por tarea**, compilando y probando antes de cada uno.
- **Verificación automática**: 216 pruebas (115 dominio, 20 aplicación, 11 arquitectura con ArchUnitNET,
  70 del simulador), 0 advertencias, SonarQube y CI en cada push.
- **La devolución del docente como insumo**: de ahí salieron la pantalla de Ayuda, el cierre de
  mantenimiento y el criterio de escribir para quien opera el parque.

## 9. Si prefieren grabar en local en vez de producción

Ventajas: pueden apagar el MCP para mostrar el error, forzar fallas sin tocar el servidor y no dependen
de la red. Pasos:

```bash
git clone --branch main --recurse-submodules https://github.com/6buriano/.NetSolarUY.git
cd .NetSolarUY
cp .env.example .env          # completar la clave de Keycloak y la de Gemini
docker compose up -d
docker compose --profile migracion run --rm migracion
```

Después entrar por `http://localhost:8080`. La guía completa está en
`Documentacion/PRUEBA-RAPIDA-SolarUY.md`.
