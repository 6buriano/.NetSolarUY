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

# Qué NO se analiza (código que no escribimos a mano o que no es de SolarUY):
#  - Migrations: las genera EF Core.
#  - wwwroot/lib: Bootstrap y jQuery de la plantilla MVC.
#  - SolarUY.SensorSimulator: es el mock del hardware, un repositorio aparte (submódulo).
EXCLUSIONES="**/Migrations/**,**/wwwroot/lib/**,SolarUY.SensorSimulator/**"

# 1. Inicia el escáner. qualitygate.wait hace que "end" espere el veredicto del
#    Quality Gate y termine con error si no pasa (así el script sirve de guardarrail).
dotnet sonarscanner begin \
  /k:"$PROJECT_KEY" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.exclusions="$EXCLUSIONES" \
  /d:sonar.qualitygate.wait=true

# 2. Compila: el escáner se engancha al compilador para analizar el código.
echo "Compilando..."
dotnet build SolarUY.slnx --no-incremental

# 3. Envía los resultados y espera el Quality Gate.
echo "Enviando resultados..."
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"

echo "========================================="
echo "Análisis terminado. Quality Gate aprobado."
echo "========================================="
