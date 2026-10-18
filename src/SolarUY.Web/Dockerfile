# Imagen de SolarUY.Web (ASP.NET Core MVC).
# El contexto de build es la RAÍZ del repo:  docker compose build web1
# Se publica el .csproj y no SolarUY.slnx, porque la solución referencia el submódulo del
# simulador y ese no tiene que entrar en la imagen.

# ---- build ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los .csproj: si cambia únicamente el código, la capa de restore se reusa.
COPY src/SolarUY.Domain/SolarUY.Domain.csproj src/SolarUY.Domain/
COPY src/SolarUY.Application/SolarUY.Application.csproj src/SolarUY.Application/
COPY src/SolarUY.Infrastructure/SolarUY.Infrastructure.csproj src/SolarUY.Infrastructure/
COPY src/SolarUY.Web/SolarUY.Web.csproj src/SolarUY.Web/
RUN dotnet restore src/SolarUY.Web/SolarUY.Web.csproj

COPY src/ src/
RUN dotnet publish src/SolarUY.Web/SolarUY.Web.csproj -c Release -o /app/publish --no-restore

# ---- runtime -------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl: la imagen oficial no lo trae y lo usa el healthcheck del compose contra /health.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Carpeta de claves de Data Protection. El volumen compartido hereda el dueño de esta
# carpeta, así que tiene que existir y pertenecer al usuario de la app (no root).
RUN mkdir -p /var/solaruy/dataprotection \
    && chown $APP_UID:$APP_UID /var/solaruy/dataprotection

COPY --from=build /app/publish .

EXPOSE 8080

# Las imágenes oficiales de .NET ya traen el usuario "app" (UID 1654): la app no corre como root.
USER $APP_UID

ENTRYPOINT ["dotnet", "SolarUY.Web.dll"]
