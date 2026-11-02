# Imagen del analizador: SDK de .NET 10 + Java (lo pide SonarScanner) + dotnet-sonarscanner.
# Así no hace falta instalar nada en la máquina: todo corre en Docker.
FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends default-jre \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

RUN dotnet tool install --global dotnet-sonarscanner
ENV PATH="$PATH:/root/.dotnet/tools"

COPY entrypoint.sh /entrypoint.sh
# sed: si el repo se clonó en Windows con finales de línea CRLF, bash no puede ejecutar el script.
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

# Acá se monta el código del repo (docker run -v <repo>:/app).
WORKDIR /app
ENTRYPOINT ["/entrypoint.sh"]
