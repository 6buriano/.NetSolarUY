# Imagen del analizador: SDK de .NET 10 + Java (lo pide SonarScanner) + dotnet-sonarscanner.
# Así no hace falta instalar nada en la máquina: todo corre en Docker.
FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends default-jre \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home scanner

COPY entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

USER scanner
ENV HOME=/home/scanner
RUN dotnet tool install --global dotnet-sonarscanner
ENV PATH="$PATH:/home/scanner/.dotnet/tools"

WORKDIR /app
ENTRYPOINT ["/entrypoint.sh"]