# Analiza el repo con SonarQube. Uso (desde la raiz del repo, en PowerShell):
#   $env:SONAR_TOKEN = "squ_xxx"
#   .\tools\SonarQube\analizar.ps1 .
# El token NO se escribe en ningun archivo: se pasa por variable de entorno.
param([string]$ProjectPath = $PWD.Path)

$ErrorActionPreference = "Stop"
$ProjectKey = if ($env:PROJECT_KEY) { $env:PROJECT_KEY } else { "SolarUY" }
$SonarUrl = "http://localhost:9000"   # desde esta maquina
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

if (-not $env:SONAR_TOKEN) {
    Write-Host "ERROR: defini `$env:SONAR_TOKEN (SonarQube > My Account > Security > Generate Tokens)." -ForegroundColor Red
    exit 1
}
try { $AbsPath = (Resolve-Path $ProjectPath).Path } catch {
    Write-Host "ERROR: no existe $ProjectPath" -ForegroundColor Red
    exit 1
}
try {
    $estado = Invoke-RestMethod -Uri "$SonarUrl/api/system/status"
    if ($estado.status -ne "UP") { throw "estado: $($estado.status)" }
} catch {
    Write-Host "ERROR: SonarQube no responde en $SonarUrl ($_)." -ForegroundColor Red
    Write-Host "Levantalo con: docker compose -f tools/SonarQube/docker-compose.yml up -d"
    exit 1
}

# Quality Gate estricto (el del taller): sin issues Blocker y con seguridad y confiabilidad en A.
# Si ya existe, SonarQube responde error y se ignora.
$Encoded = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($env:SONAR_TOKEN):"))
$Headers = @{ Authorization = "Basic $Encoded" }
$QG = "EstrictoTaller"
function Api($ruta) {
    try { Invoke-RestMethod -Uri "$SonarUrl/api/qualitygates/$ruta" -Method Post -Headers $Headers | Out-Null } catch {}
}
Api "create?name=$QG"
Api "create_condition?gateName=$QG&metric=blocker_violations&op=GT&error=0"
Api "create_condition?gateName=$QG&metric=security_rating&op=GT&error=1"
Api "create_condition?gateName=$QG&metric=reliability_rating&op=GT&error=1"
Api "set_as_default?name=$QG"

$Image = "solaruy-scanner"
if (-not (docker images -q $Image)) {
    Write-Host "Construyendo la imagen del analizador (solo la primera vez)..." -ForegroundColor Yellow
    docker build -t $Image "$ScriptDir/scanner"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# host.docker.internal: como el contenedor llega al SonarQube de esta maquina.
docker run --rm `
  --add-host=host.docker.internal:host-gateway `
  -v "${AbsPath}:/app" `
  -e SONAR_HOST_URL="http://host.docker.internal:9000" `
  -e SONAR_TOKEN="$($env:SONAR_TOKEN)" `
  -e PROJECT_KEY="$ProjectKey" `
  $Image
exit $LASTEXITCODE
