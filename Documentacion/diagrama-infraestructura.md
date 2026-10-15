# Diagrama de infraestructura — SolarUY

Arquitectura **objetivo** del sistema: es la que persiguen las iteraciones 2 y 3. En la iteración 1 todo
corre en un solo equipo (MySQL y el simulador en Docker, la aplicación y el colector con `dotnet run`).

El código Mermaid de abajo es la fuente del diagrama: se puede pegar en
[mermaid.live](https://mermaid.live) o renderizar con la extensión de Mermaid de VS Code. La imagen
renderizada que se usa en la entrega está en `entrega-1-diagramas/diagrama-infraestructura.png`.

```mermaid
graph TD
    User([Usuario / Navegador Web]) -->|HTTP/HTTPS / WebSockets| LB[Balanceador de Carga: Nginx / YARP]

    subgraph VPS Oracle Cloud OCI
        LB -->|Round-Robin / Sticky Sessions| App1
        LB -->|Round-Robin / Sticky Sessions| App2

        subgraph "Instancias de la Aplicación (Réplicas)"
            direction LR
            App1[Instancia App MVC 1<br/>.NET Core + SignalR]
            App2[Instancia App MVC 2<br/>.NET Core + SignalR]
        end

        subgraph Capa de Seguridad y Datos
            KC[Keycloak<br/>OpenID Connect]
            DB[(Base de Datos<br/>MySQL)]
            Redis[(Redis Backplane<br/>Sincronización SignalR)]
        end

        subgraph Simulador y Servicios
            Sim[Simulador HTTP<br/>Mock inversor/sensor ARM]
            Col[Colector de telemetría<br/>Worker Service]
            MCP[Servidor / Endpoint MCP<br/>Herramientas de Consulta]
        end
    end

    subgraph Proveedor IA Externo
        Gemini[Google Gemini API]
    end

    %% Conexiones internas
    App1 -->|OIDC Auth| KC
    App2 -->|OIDC Auth| KC

    App1 -->|EF Core| DB
    App2 -->|EF Core| DB

    App1 <--->|Pub/Sub Eventos| Redis
    App2 <--->|Pub/Sub Eventos| Redis

    Col -->|GET /sensores/{hw}/estado| Sim
    Col -->|EF Core: lecturas_telemetria| DB
    Col -->|Push métricas| App1
    Col -->|Push métricas| App2

    App1 -->|Herramientas / Contexto DB| MCP
    App2 -->|Herramientas / Contexto DB| MCP
    MCP <--->|Integración LLM| Gemini
```

Nota: el simulador **no** escribe en la base. Es una caja negra por HTTP, así que el que consulta los
sensores y guarda las lecturas es el **colector**, que es el nodo que se agregó al diagrama original de
referencia.
