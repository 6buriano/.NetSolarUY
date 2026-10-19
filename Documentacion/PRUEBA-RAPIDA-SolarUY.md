# Prueba rápida — SolarUY (ABM + telemetría)

Guía para levantar el proyecto y hacer una prueba corta de la telemetría, sin leer todo el código.
Tiempo estimado: **15 minutos la primera vez** (casi la mitad se va compilando la imagen del simulador).

> ✅ **Ya está pusheado.** Todo esto está en la rama `main`. Verificado desde un clon limpio: baja el
> submódulo solo, `dotnet build` termina sin errores ni advertencias y los **153 tests** pasan.
>
> Asegurate de estar en el **último commit de la rama** (`git pull`): la tabla de "Últimas 20 lecturas" del
> detalle del panel llegó en un commit posterior.
>
> Si preferís clonar de cero en lugar de cambiar de rama, esta es la forma más simple, porque trae el
> submódulo de una:
>
> ```bash
> git clone --branch main --recurse-submodules https://github.com/6buriano/.NetSolarUY.git
> ```

---

## Requisitos

| Necesitás | Cómo lo comprobás |
|---|---|
| **.NET SDK 10** | `dotnet --version` → tiene que decir `10.x` |
| **Docker Desktop** | Tiene que estar **abierto y corriendo**, no alcanza con tenerlo instalado |
| **Git** | `git --version` |

---

## Paso a paso

### 1. Traer la rama y el submódulo

Si ya tenés el repositorio clonado:

```bash
git fetch origin
git checkout main
git pull
git submodule update --init
```

El proyecto del simulador es un **submódulo**: git no trae su contenido solo. Si te olvidás del último
comando, `dotnet build` falla porque la solución referencia proyectos que no existen en tu disco
(vas a ver la carpeta `SolarUY.SensorSimulator` vacía).

Comprobación: adentro de `SolarUY.SensorSimulator` tiene que haber archivos, no estar vacía.

### 2. Levantar el sistema

Hay dos modos. **Elegí uno y seguí con el paso 3.**

**Modo Docker (recomendado: es la topología de la entrega).** Levanta Nginx, las dos réplicas de la web, el
colector y MySQL:

```powershell
Copy-Item .env.example .env
docker compose up -d --wait mysql
docker compose --profile migracion run --rm migracion
docker compose up -d --wait
```

Entrás por `http://localhost:8080` (`web1` atiende, `web2` queda de reserva). La primera vez tarda varios
minutos porque descarga imágenes y compila.

**Modo host (`dotnet run`).** Solo MySQL y el simulador local en Docker; la web y el colector en tu máquina:

```powershell
docker compose up -d --wait mysql
docker compose --profile simulador-local up -d
```

El simulador local queda escuchando en `http://localhost:5100`. Comprobación rápida de que está vivo:

```powershell
curl.exe http://localhost:5100/simulador/inventario
```

Tenés que ver `ARM-001`, `ARM-002` y `ARM-003`, cada uno con sus paneles `P01`…`P05`. En modo Docker se usa
el simulador desplegado (`SIMULADOR_URL` en `.env`) y este paso no hace falta.

### 3. Herramientas y compilación

```bash
dotnet tool restore
dotnet build
```

`dotnet build` tiene que terminar con **0 errores y 0 advertencias**.

### 4. Levantar la web (dejá esta terminal abierta)

> En **modo Docker** este paso y el 6 no van: la web y el colector ya están corriendo. Abrí
> `http://localhost:8080` y seguí desde el paso 5.

```bash
dotnet run --project src/SolarUY.Web
```

Cuando diga `Now listening on: http://localhost:5192`, abrí esa dirección.
La primera vez aplica las migraciones solo, así que **no hace falta correr `dotnet ef database update`**.

> Si es la primera vez que compila, puede tardar un rato y quedarse en `Compilando...`. Esperá a ver
> `Application started`. No lo cortes antes.

### 5. Cargar los datos de la demo (en la web)

El sistema es la fuente de verdad de parques, nodos y paneles, pero el simulador **solo conoce códigos de
hardware**. Hay que hacerlos coincidir. Con la configuración por defecto el simulador tiene 3 sensores
(`ARM-001`…`ARM-003`) con 5 paneles cada uno (`P01`…`P05`) de **300 W**.

1. **Parques → Nuevo parque**: cualquiera (ej. "Parque Norte", "Salto", 250.5 kW).
2. **Nodos → Nuevo nodo**. Al final del formulario está la sección **Sensor ARM del nodo** (es opcional):
   - Identificador de hardware: `ARM-001`
   - Dirección del sensor: viene **precompletada** con la que corresponde al modo elegido
     (`http://simulador.fabriq.uy` en Docker, `http://localhost:5100` con `dotnet run`). No la escribas a
     mano.
   - Firmware: lo que quieras, y dejá "Sensor activo" tildado.

   Si escribís un identificador que el simulador **no** emula (por ejemplo `ARM-009`), el formulario no te
   deja con un nodo muerto: te avisa cuáles existen y te ofrece **crear el sensor** en el simulador o
   **crear el nodo sin sensor**. Si elegís crearlo, acordate de cargarle después los paneles que te
   indique, porque el alta del nodo **no** crea los paneles.
3. **Paneles → Nuevo panel**, cinco veces sobre ese nodo:
   - Código de panel: `P01`, `P02`, `P03`, `P04`, `P05`
   - Potencia nominal: `300`
   - Modelo y fabricante: lo que quieras.

Los datos que tienen que coincidir con el simulador son cuatro: el **identificador de hardware** y la
**dirección** del sensor, y en cada panel el **código** y la **potencia nominal**. Si el código no coincide,
el colector ignora ese panel (y te lo avisa en el log); si la potencia no coincide, la comparación entre
paneles del mismo nodo queda distorsionada.

### 6. Arrancar el colector (en **otra** terminal)

> En **modo Docker** ya está corriendo. Verificá con `docker compose logs --tail 10 collector` que aparezca
> `Ronda de telemetría: 5 lecturas guardadas`.

```bash
dotnet run --project src/SolarUY.Collector
```

Tiene que aparecer esto cada 5 segundos:

```
Colector de telemetría iniciado: una ronda cada 5 segundos.
Ronda de telemetría: 15 lecturas guardadas.
Ronda de telemetría: 15 lecturas guardadas.
```

Ese `15` son tus 15 paneles (3 nodos × 5). Ya está entrando telemetría a MySQL.

### 7. Ver el resultado

En la web: **Paneles → botón "Ver"** en cualquier panel. Vas a ver dos cosas:

- **"Última lectura de telemetría"**: el dato más reciente (corriente, voltaje, potencia, temperatura y
  estado detectado).
- **"Últimas 20 lecturas"**: la serie que fue guardando el colector, de la más nueva a la más vieja. Las
  filas en rojo son lecturas en falla y en amarillo, rendimiento bajo. Las horas están en **hora de
  Montevideo**.

### 8. Verificar que los datos coincidan con el hardware

En la barra de arriba hay un **Diagnóstico** (solo aparece en desarrollo). Compara tus nodos, sensores y
paneles contra el inventario del simulador y lista **todo lo que no cierra**:

- Un **error** (rojo) significa que ese nodo o panel nunca va a recibir telemetría: por ejemplo, un sensor
  con un identificador que el simulador no emula, o un panel con un código que el sensor no informa.
- Una **advertencia** (amarillo) no rompe la recolección pero conviene corregirla: por ejemplo, un panel
  cargado con 550 W cuando el sensor genera hasta 300 W, o un panel que el sensor informa y el sistema no
  tiene cargado.
- Abajo te dice **cuántos sensores libres quedan**: son los identificadores que podés usar para crear nodos
  nuevos. Si dice que no quedan, hay que agregar sensores al simulador (ver los problemas típicos más abajo).

Conviene mirarla **después de cargar los datos y antes de la demo**: es la forma rápida de saber si lo que
cargaste se corresponde con el hardware. Es un informe: esa pantalla no da de alta nada.

**Si no quedan sensores libres y querés otro nodo**, no hace falta salir de la pantalla de nodos: cargá en
**Nodos → Nuevo nodo** el identificador que quieras (por ejemplo `ARM-004`) y el formulario te va a ofrecer
**crearlo en el simulador**. El sistema le pide al simulador que empiece a emularlo, crea el nodo con ese
sensor y te dice **qué paneles hay que cargarle** (`P01` a `P05`): el alta **no crea los paneles**, así que el
nodo queda sin lecturas hasta que los cargues. La otra salida que ofrece el formulario es **crear el nodo sin
sensor**: queda cargado, pero no recibe telemetría hasta que le pongas uno.

Dos advertencias sobre eso:

- El sensor que se crea **vive en la memoria del simulador**: si reiniciás el contenedor, desaparece y el nodo
  queda sin telemetría. Para que quede hay que agregarlo al `docker-compose.yml`
  (`Simulador__NodosIniciales`) y reiniciar el contenedor.
- Es una comodidad de desarrollo: con hardware real no existe, porque un sensor se instala, no se crea por
  HTTP. Fuera del entorno de Development el sistema no verifica nada y crea el nodo con el identificador que
  le des.

---

## La prueba corta: forzar una falla

En una tercera terminal (o la que quieras), con todo lo anterior corriendo:

> Si usás **Windows PowerShell 5.1** (el de fondo azul, no PowerShell 7), escribí `curl.exe` en lugar de
> `curl`: ahí `curl` es un alias de `Invoke-WebRequest` y estos comandos no funcionan igual. En PowerShell 7,
> en Git Bash o en la terminal de VS Code, `curl` es el de verdad.

```bash
# 1. Forzar sobretemperatura en el panel P01 del sensor ARM-001
curl -X POST "http://localhost:5100/simulador/fallas/ARM-001/P01?tipo=sobretemperatura"

# 2. Esperar ~6 segundos (una ronda del colector)

# 3. Mirar la web: Paneles -> Ver en el P01 -> tiene que decir FALLA_DETECTADA
#    y la temperatura arriba de 80 grados

# 4. Levantar la falla
curl -X DELETE http://localhost:5100/simulador/fallas/ARM-001/P01

# 5. Esperar otra ronda: el panel vuelve solo a OPERATIVO
```

También podés probar `?tipo=caidaPotencia`, que baja la potencia del panel a la mitad. La diferencia es
interesante: la caída se evalúa **contra el promedio de los otros paneles activos del mismo nodo**, no
contra la potencia nominal. Eso está explicado en la sección "Cómo entra la telemetría" del `README.md`.

---

## Cuando terminen

```bash
# Ctrl+C en la terminal del colector
# Ctrl+C en la terminal de la web
docker compose stop
```

⚠️ **Pará el colector cuando termines de mirar.** Escribe 15 filas cada 5 segundos: son unas
**260.000 filas por día**. Todavía no hay política de retención.

---

## Cosas que les pueden pasar

| Síntoma | Qué es |
|---|---|
| `dotnet build` falla con *"The process cannot access the file … because it is being used by another process"* | Tenés la web corriendo y bloquea los `.dll`. Parala con `Ctrl+C` antes de recompilar. |
| `dotnet run` queda en `Compilando…` y parece colgado | Es la primera compilación. Esperá a `Application started`. Me pasó a mí y creí que estaba roto. |
| **Todos los paneles en 0 W** | Es de noche. La curva solar del simulador es cero fuera de las 6:00 y las 18:00, así que de noche no hay generación. **No está roto**: la regla de temperatura sigue funcionando, y por eso la prueba de arriba usa sobretemperatura. |
| Quieren ver generación de verdad sin esperar al mediodía | Apunten el simulador a una zona horaria donde sea de día (probado: da ~220 W por panel de 300 W): <br>`docker rm -f solaruy-sensor-simulator` <br>*esperen 4 segundos, Docker tarda en liberar el puerto 5100* <br>`docker run -d --name sim-dia -p 127.0.0.1:5100:8080 -e Simulador__ZonaHoraria=Asia/Shanghai solaruy-sensor-simulator:latest` <br>Para volver: `docker rm -f sim-dia` y después `docker compose up -d sensor-simulator`. |
| El colector no guarda nada y no dice nada | Fijate que el nodo tenga sensor **activo**, con la URL y el identificador que existen en el simulador. Si un panel no coincide por código, el log lo avisa. |
| El log del colector repite *"El sensor ARM-00X no existe"* | Cargaste un identificador que el simulador no emula. Mirá la pantalla de **Diagnóstico**, que te lo dice junto con todo lo demás. Para crear el sensor: dá de alta un nodo con ese identificador (el formulario te ofrece crearlo en el simulador) o a mano: `curl.exe -X POST http://localhost:5100/simulador/sensores -H "Content-Type: application/json" -d '{"identificadorHardwareARM":"ARM-004","cantidadPaneles":5,"potenciaNominalW":300}'` |
| No te deja crear más nodos: *"El identificador de hardware es único"* | Ese identificador ya lo usa otro nodo. Mirá **Diagnóstico**: te dice cuántos sensores libres quedan. Si no queda ninguno, en **Nodos → Nuevo nodo** escribí el que quieras (por ejemplo `ARM-004`): el formulario te ofrece crearlo en el simulador, y acordate de cargarle después los paneles que te indique. También podés subir `Simulador__NodosIniciales` en `docker-compose.yml` y reiniciar el contenedor. |
| Un panel nunca recibe lecturas y el log no dice nada | El colector solo avisa cuando el sensor informa un panel que el sistema no tiene; al revés no. La pantalla de **Diagnóstico** sí lo detecta en los dos sentidos. |
| `docker compose up` falla en `sensor-simulator` | No inicializaste el submódulo: `git submodule update --init`. |
| La web no arranca: *"Falta la cadena de conexión 'SolarUY'"* | Se corrió fuera del perfil de Development. Usá `dotnet run --project src/SolarUY.Web` tal cual, sin `--no-launch-profile`. |
| El puerto 5100 está ocupado | Ya tenés otro simulador levantado: `docker ps` y pará el que sobra. |

---

## Qué está funcionando y qué no

**Funciona y está probado:**

- ABM completo de Parques, Nodos y Paneles (listado con filtros, alta, edición, detalle y baja).
- Las reglas de negocio: un nodo con exactamente un inversor, número de serie y sensor únicos, no se puede
  borrar un parque con nodos ni un nodo con paneles, y el código de panel es único dentro del nodo.
- Telemetría de punta a punta: el colector consulta al simulador, el dominio diagnostica la salud y las
  lecturas quedan en MySQL.
- La regla de estado de la spec (§9): `DESCONECTADO` para paneles inactivos, `FALLA_DETECTADA` arriba de
  75 °C, y la caída de potencia medida contra el promedio del nodo en tres bandas (15 % / 40 %).
- La serie de las últimas 20 lecturas de cada panel, en su pantalla de detalle, con las horas en hora de
  Montevideo.
- El alta de un nodo con un sensor ARM que el simulador no emula: el formulario avisa cuáles existen y
  ofrece crearlo en el simulador (y el nodo queda con ese sensor, listo para cargarle los paneles) o crear
  el nodo sin sensor.
- La pantalla de **Diagnóstico**, que compara el sistema contra el inventario del simulador y te dice qué
  no cierra y cuántos identificadores de sensor quedan libres.
- 153 tests automáticos (83 del dominio + 70 del simulador). Corré `dotnet test`.

**Todavía no:**

- **No hay gráficos ni dashboard.** La serie se ve como tabla en el detalle de cada panel, pero no hay una
  pantalla de monitoreo con gráficos, métricas por día/mes ni actualización automática por SignalR.
- **Las órdenes al sensor no están cableadas a la interfaz** (activar/desactivar un panel o el volcado a
  red). El cliente ya las implementa; falta el botón.
- **No hay autenticación.** Falta Keycloak y el `[Authorize(Roles = "Administrador")]` sobre el ABM.
- El chat con IA no está empezado.

---

Cualquier cosa que les falle, manden el mensaje de error completo y lo miramos.
