#!/bin/bash
# Analiza el repo con SonarQube. Uso (desde la raíz del repo):
#   SONAR_TOKEN=squ_xxx ./tools/SonarQube/analizar.sh .
# El token NO se escribe en ningún archivo: se pasa por variable de entorno.
set -e

PROJECT_PATH="${1:-$(pwd)}"
ABS_PATH="$(realpath "$PROJECT_PATH")"
PROJECT_KEY="${PROJECT_KEY:-SolarUY}"
SONAR_URL_HOST="http://localhost:9000"   # desde esta máquina
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -z "$SONAR_TOKEN" ]; then
    echo "ERROR: definí SONAR_TOKEN (SonarQube > My Account > Security > Generate Tokens)."
    exit 1
fi
if [ ! -d "$ABS_PATH" ]; then
    echo "ERROR: no existe $ABS_PATH"
    exit 1
fi
if ! curl -fsS "$SONAR_URL_HOST/api/system/status" | grep -q '"status":"UP"'; then
    echo "ERROR: SonarQube no responde en $SONAR_URL_HOST."
    echo "Levantalo con: docker compose -f tools/SonarQube/docker-compose.yml up -d"
    exit 1
fi

# Quality Gate estricto (el del taller): sin issues Blocker y con seguridad y confiabilidad en A.
# Si ya existe, SonarQube responde error y se ignora.
QG="EstrictoTaller"
api() { curl -s -u "${SONAR_TOKEN}:" -X POST "$SONAR_URL_HOST/api/qualitygates/$1" > /dev/null || true; }
api "create?name=$QG"
api "create_condition?gateName=$QG&metric=blocker_violations&op=GT&error=0"
api "create_condition?gateName=$QG&metric=security_rating&op=GT&error=1"
api "create_condition?gateName=$QG&metric=reliability_rating&op=GT&error=1"
api "set_as_default?name=$QG"

IMAGE="solaruy-scanner"
if [ -z "$(docker images -q $IMAGE)" ]; then
    echo "Construyendo la imagen del analizador (solo la primera vez)..."
    docker build -t $IMAGE "$SCRIPT_DIR/scanner"
fi

# host.docker.internal: cómo el contenedor llega al SonarQube de esta máquina.
# En Linux hace falta el add-host; en Docker Desktop (Windows/Mac) ya existe y no molesta.
docker run --rm \
  --add-host=host.docker.internal:host-gateway \
  -v "$ABS_PATH":/app \
  -e SONAR_HOST_URL="http://host.docker.internal:9000" \
  -e SONAR_TOKEN="$SONAR_TOKEN" \
  -e PROJECT_KEY="$PROJECT_KEY" \
  $IMAGE
