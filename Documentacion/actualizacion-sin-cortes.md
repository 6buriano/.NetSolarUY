# Actualización sin cortes — SolarUY

Cómo instalar una versión nueva del sistema sin dejar de atender, con la topología que entrega el
`docker-compose.yml`: **Nginx** al frente, dos réplicas idénticas de la web (`web1` principal, `web2` en
`backup`), un **colector** único, MySQL y el simulador.

Qué garantiza y qué no:

- **Garantiza** que ninguna petición quede sin respuesta durante el despliegue: todas las peticiones del
  cliente devuelven 200. Lo que se paga es **latencia**, no errores (ver §7).
- **No garantiza** disponibilidad ante la caída del servidor: Nginx, las réplicas y MySQL viven en un solo
  VPS. Para eso haría falta un segundo nodo, que queda fuera de esta entrega.

Todos los comandos son de PowerShell (Windows) y se ejecutan desde la raíz del repositorio.

## 1. Piezas que importan para actualizar

| Pieza | Por qué importa al actualizar |
|---|---|
| `nginx/nginx.conf` | `upstream` activo/pasivo con `zone` compartida. **Resuelve los nombres al arrancar y no los vuelve a resolver solo.** |
| `web1` / `web2` | Réplicas idénticas y sin estado en el servidor: comparten el anillo de claves de Data Protection en un volumen. |
| `migracion` | Paso de un solo uso: aplica el esquema **y crea el anillo de claves** antes de que arranquen las réplicas. |
| `collector` | Una sola instancia. Nunca se escala: dos colectores escribirían las mismas lecturas dos veces. |

## 2. Antes de empezar

1. **Las migraciones tienen que ser compatibles hacia atrás** (`expand`/`contract`). Durante la
   actualización conviven la versión vieja y la nueva contra el mismo esquema: una columna nueva con valor
   por defecto entra, pero renombrar o borrar algo que la versión vieja todavía usa rompe la actualización
   sin cortes.
2. **Etiquetar la imagen que está corriendo**, para poder volver atrás:

   ```powershell
   docker tag solaruy-web:latest solaruy-web:anterior
   docker tag solaruy-collector:latest solaruy-collector:anterior
   ```

3. **No saltear el paso `migracion`**: es el que crea el anillo de claves una sola vez. Sin él, con el
   volumen vacío cada réplica genera su propia clave y un failover rechaza tokens (ver §7).

## 3. Puesta en marcha desde cero

```powershell
Copy-Item .env.example .env
docker compose up -d --wait mysql
docker compose --profile migracion run --rm migracion
docker compose up -d --wait
```

Verificación:

```powershell
curl.exe -fsS http://localhost:8080/health
docker compose exec web1 ls -ln /var/solaruy/dataprotection
```

- `/health` tiene que devolver `{"status":"ok","machine":"web1"}`.
- En el volumen de claves tiene que haber **un solo** `key-*.xml`, dueño `1654 1654`.

## 4. La actualización, paso a paso

```powershell
docker compose build web1 web2
docker compose --profile migracion run --rm migracion
docker compose up -d --no-deps web2
docker compose exec web2 curl -fsS http://127.0.0.1:8080/health
docker compose exec nginx nginx -s reload
docker compose up -d --no-deps web1
docker compose exec web1 curl -fsS http://127.0.0.1:8080/health
docker compose exec nginx nginx -s reload
curl.exe -fsS http://localhost:8080/health
```

El orden no es arbitrario:

1. **`build`** deja la imagen nueva en el registro local; los contenedores siguen con la vieja hasta que se
   recrean.
2. **`migracion`** aplica el esquema (y asegura el anillo) una sola vez, antes de que haya réplicas nuevas.
3. **`web2` primero**, la reserva: mientras no haya tráfico repartiéndose, es la que no atiende a nadie.
4. Se verifica **llamándola directo** (`127.0.0.1` dentro del contenedor), no por Nginx, que todavía manda
   todo a `web1`.
5. **`reload` de Nginx** antes de tocar la principal.
6. **`web1` después**: al recrearla, Nginx la da por caída tras `max_fails` fallos y atiende con `web2`.
7. **`reload` final** para que Nginx vuelva a resolver la IP nueva de `web1` y la prefiera otra vez.

> **Después de recrear una réplica, recargar Nginx antes de pasar a la siguiente.** Nginx resuelve los
> nombres del `upstream` al arrancar y no los vuelve a resolver solo: si se recrean las dos sin recargar, la
> IP vieja de la reserva queda en caché y, cuando la principal se detiene, no hay ninguna réplica
> alcanzable.

## 5. Cómo verificarlo

Un bucle en segundo plano que mide las dos cosas que importan: el health y una página, con código y
tiempo. Dejalo en una ruta fija (no en `$env:TEMP`: cada terminal puede tener uno distinto).

```powershell
$log = 'E:\SolarUY-ha.log'
$fin = (Get-Date).AddMinutes(5)
while ((Get-Date) -lt $fin) {
  $h = curl.exe -s -o NUL -w "%{http_code}|%{time_total}" http://localhost:8080/health
  $p = curl.exe -s -o NUL -w "%{http_code}|%{time_total}" http://localhost:8080/
  "{0} health={1} page={2}" -f (Get-Date -Format "HH:mm:ss.fff"), $h, $p | Add-Content $log
  Start-Sleep -Milliseconds 250
}
```

Criterios: **cero respuestas distintas de 200** y lentitud acotada al momento en que se recrea la réplica
que estaba atendiendo.

## 6. Resultado medido (18/10/2026)

Dos corridas completas de la secuencia, con un cambio de prueba sin esquema (un texto en el pie de página):

| Corrida | Peticiones | No-200 | Lentas (> 0,5 s) | Dónde |
|---|---|---|---|---|
| Actualización | 610 | **0** | **1** (1,005 s) | ~1 s después de arrancar `web1` |
| Cierre (revertir) | 490 | **0** | **1** (1,010 s) | fase de recrear `web1`; en la de `web2`, máximo 9 ms |

- Las dos réplicas recuperaron **la misma IP** al recrearse (`web1` `10.201.0.3`, `web2` `10.201.0.4`), así
  que el riesgo de la IP vieja no se materializó. El `reload` igual va: no está garantizado.
- El `build`, la migración y la recreación de la **reserva** no produjeron **ninguna** petición lenta.
- El token del cambio de prueba apareció primero sirviendo `web2` y después `web1`, en ese orden.

## 7. Limitaciones conocidas

**(a) Claves sin cifrar.** Data Protection guarda el anillo **sin cifrar** en el volumen; el framework lo
avisa con `XmlKeyManager[35]`. Es una decisión consciente: no se configuró `ProtectKeysWithCertificate`
porque el riesgo asumido es el acceso al volumen, no el disco del host, y cerrarlo exige emitir y rotar un
certificado.

**(b) Costo del failover.** Mientras una réplica es inalcanzable pero sigue en rotación, cada petición paga
hasta `proxy_connect_timeout` antes de pasar a la otra. Medido con `proxy_connect_timeout 1s`: las 3
primeras peticiones tardan ~1,00 s (Nginx necesita `max_fails=3` fallos para sacarla de rotación) y el
resto ~3 ms. Con 3 s, ese tramo costaba ~3,0 s por petición. El contador vive en la `zone` del `upstream`:
sin ella es por proceso worker, no se junta entre workers y el costo se repite indefinidamente. Cada
`fail_timeout` (10 s) Nginx vuelve a sondear la réplica caída y esa petición paga otra vez el timeout. Un
proceso caído con el contenedor vivo responde con RST, así que en ese caso no hay espera.

**(c) Un solo VPS.** Las dos réplicas y Nginx corren en la misma máquina: protegen contra la caída de una
réplica y permiten actualizar sin cortes, pero no contra la caída del servidor.

**(d) Anillo de claves.** Sin sticky sessions funciona siempre que las réplicas compartan el anillo; el paso
de arranque lo crea antes de levantar las réplicas, así que en el primer arranque no hay ventana. Si se
levantan sin ese paso y con el volumen vacío, cada réplica genera su propia clave y un failover puede
rechazar tokens de la otra hasta el refresco del anillo (24 h) o hasta reiniciar la réplica.

**(e) Migraciones.** La versión nueva y la vieja conviven durante la actualización: las migraciones tienen
que ser compatibles hacia atrás y ninguna versión puede romper el esquema que la otra todavía usa.

**(f) Colector único.** Nunca se escala ni se duplica. El `container_name` hace que
`docker compose up --scale collector=2` falle en vez de duplicar lecturas. Al actualizarlo se pierde una
ronda de telemetría (5 s), que es aceptable.

## 8. Volver atrás

Con la imagen anterior etiquetada (§2) y una migración compatible:

```powershell
$env:SOLARUY_TAG = 'anterior'
docker compose up -d --no-deps web2
docker compose exec nginx nginx -s reload
docker compose up -d --no-deps web1
docker compose exec nginx nginx -s reload
Remove-Item Env:\SOLARUY_TAG
```

Si la migración **no** es compatible hacia atrás, no alcanza con volver la imagen: hay que restaurar la base
desde el backup previo, y eso sí es un corte.
