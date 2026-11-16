#!/bin/bash
set -e

for var in SONAR_HOST_URL SONAR_TOKEN PROJECT_KEY; do
    if [ -z "${!var}" ]; then
        echo "ERROR: falta la variable $var."
        exit 1
    fi
done

echo "========================================="
echo "Análisis SonarQube de: $PROJECT_KEY"
echo "Servidor: $SONAR_HOST_URL"
echo "========================================="

# El contenedor corre como usuario sin privilegios (el Dockerfile no usa root: docker:S6471) y la
# carpeta montada desde Windows llega como propiedad de root, así que ahí no se puede escribir
# (.sonarqube, bin, obj). Por eso se trabaja sobre una copia en /tmp, sin las carpetas generadas.
# Efecto colateral bueno: el repo del usuario no se ensucia con archivos del análisis.
WORK_DIR="/tmp/solaruy-src"
rm -rf "$WORK_DIR"
mkdir -p "$WORK_DIR"
tar -C /app --exclude=bin --exclude=obj --exclude=.sonarqube --exclude=node_modules -cf - . \
  | tar -C "$WORK_DIR" -xf -
cd "$WORK_DIR"

# Qué NO se analiza (código que no escribimos a mano o que no es de SolarUY):
#  - Migrations: las genera EF Core.
#  - wwwroot/lib: Bootstrap y jQuery de la plantilla MVC.
#  - SolarUY.SensorSimulator: es el mock del hardware, un repositorio aparte (submódulo).
EXCLUSIONES="**/Migrations/**,**/wwwroot/lib/**,SolarUY.SensorSimulator/**"

# Qué se sigue analizando pero NO cuenta para la cobertura:
#  - monitoreo.js y panel-detalle.js: JavaScript del front-end (123 líneas nuevas sin cubrir). El proyecto
#    no tiene herramientas de prueba de front-end y la cobertura de .NET (coverlet) no mide JavaScript.
#    Se listan los dos archivos a propósito, no la carpeta: así un JavaScript nuevo sí cuenta.
EXCLUSIONES_COBERTURA="**/wwwroot/js/monitoreo.js,**/wwwroot/js/panel-detalle.js,**/.entrega-build/**"

# Dónde coverlet deja los reportes de cobertura. Va dentro del contenedor (no en /app) para no
# ensuciar el repo del usuario con carpetas generadas. Se limpia antes por si quedó algo de una corrida previa.
COBERTURA_DIR="/tmp/solaruy-cobertura"
rm -rf "$COBERTURA_DIR"

# 1. Inicia el escáner. qualitygate.wait hace que "end" espere el veredicto del
#    Quality Gate y termine con error si no pasa (así el script sirve de guardarrail).
#    opencover.reportsPaths le dice dónde buscar la cobertura que se genera en el paso 3.
dotnet sonarscanner begin \
  /k:"$PROJECT_KEY" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.exclusions="$EXCLUSIONES" \
  /d:sonar.coverage.exclusions="$EXCLUSIONES_COBERTURA" \
  /d:sonar.cs.opencover.reportsPaths="$COBERTURA_DIR/**/coverage.opencover.xml" \
  /d:sonar.qualitygate.wait=true

# 2. Compila: el escáner se engancha al compilador para analizar el código.
echo "Compilando..."
dotnet build SolarUY.slnx --no-incremental

# 3. Corre las pruebas midiendo cobertura. Sin este paso Sonar no recibe datos y muestra 0 %.
#    Si una prueba falla, el script se corta acá (set -e): no tiene sentido evaluar la calidad
#    de código que ni siquiera pasa sus pruebas.
#    --no-build: reutiliza lo compilado en el paso 2 (con el escáner enganchado).
echo "Ejecutando pruebas con cobertura..."
dotnet test SolarUY.slnx --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COBERTURA_DIR" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# 4. Envía los resultados y espera el Quality Gate.
echo "Enviando resultados..."
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"

echo "========================================="
echo "Análisis terminado. Quality Gate aprobado."
echo "========================================="
