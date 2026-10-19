# Imagen de SolarUY.Collector (Worker Service).
# El contexto de build es la RAÍZ del repo:  docker compose build collector
# Es un worker: no expone puertos y NUNCA se escala (dos instancias duplicarían las
# lecturas en lecturas_telemetria).

# ---- build ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SolarUY.Domain/SolarUY.Domain.csproj src/SolarUY.Domain/
COPY src/SolarUY.Application/SolarUY.Application.csproj src/SolarUY.Application/
COPY src/SolarUY.Infrastructure/SolarUY.Infrastructure.csproj src/SolarUY.Infrastructure/
COPY src/SolarUY.Collector/SolarUY.Collector.csproj src/SolarUY.Collector/
RUN dotnet restore src/SolarUY.Collector/SolarUY.Collector.csproj

COPY src/ src/
RUN dotnet publish src/SolarUY.Collector/SolarUY.Collector.csproj -c Release -o /app/publish --no-restore

# ---- runtime -------------------------------------------------------------------------
# runtime y no aspnet: el colector no es una aplicación web.
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Mismo criterio que la web: usuario "app" de la imagen, no root.
USER $APP_UID

ENTRYPOINT ["dotnet", "SolarUY.Collector.dll"]
