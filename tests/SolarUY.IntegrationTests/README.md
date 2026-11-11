# Pruebas de integración

Verifican la **ingesta** y las **consultas** contra un MySQL real, con el esquema de las migraciones:

- `IngestaTelemetriaTests`: corre el `ColectorTelemetriaWorker` real con un sensor de mentira. Comprueba que se
  guarda una lectura por panel, que el dominio diagnostica (falla por temperatura, panel desconectado), que se
  sincroniza el volcado a la red y que un sensor caído o un panel desconocido no frenan al resto.
- `ConsultasHistoricasTests`: `ReporteService` y el repositorio con lecturas de fechas conocidas. Comprueba el
  agrupamiento por día/mes/año, el rango de fechas, el filtro por parque, "hoy" en hora de Montevideo, la
  energía estimada y la purga de retención.

Se usa MySQL y no una base en memoria porque lo que se prueba depende del motor (los `GroupBy` por fecha, el
`ExecuteDelete`, las migraciones).

## Cómo correrlas

Hace falta un MySQL con un usuario que pueda crear bases. Cada prueba crea la suya (`solaruy_it_<guid>`) y la
borra al terminar, así que no toca datos de desarrollo.

```bash
docker run -d --name solaruy-test-mysql -e MYSQL_ROOT_PASSWORD=test -p 127.0.0.1:3307:3306 mysql:8.4

SOLARUY_TEST_MYSQL="Server=127.0.0.1;Port=3307;User=root;Password=test;" \
  dotnet test tests/SolarUY.IntegrationTests
```

Sin la variable `SOLARUY_TEST_MYSQL`, las pruebas se **saltean** con un aviso (no fallan). En el CI la variable
está definida, así que ahí corren siempre.
