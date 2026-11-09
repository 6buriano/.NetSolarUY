# Diagrama de infraestructura — SolarUY

Arquitectura del sistema. El despliegue actual corre todo en Docker en un solo equipo (`docker compose up -d`):
Nginx, las dos réplicas de la web, el colector, el servidor MCP, MySQL y el simulador. De lo que todavía
figura como objetivo de las iteraciones 2 y 3 queda **Keycloak**, que hoy se usa como servicio externo
(`auth.fabriq.uy`).

El **servidor MCP** (`SolarUY.McpServer`) es un proceso aparte que expone cinco herramientas de solo lectura
por HTTP sin estado en `/mcp`; las réplicas de la web lo consultan dentro de la red de Docker. **Gemini** es un
servicio **externo** (API de Google): la web le manda la pregunta junto con las herramientas y el servidor MCP
nunca lo alcanza, porque solo conoce la base.

El código Mermaid de abajo es la fuente del diagrama: se puede pegar en
[mermaid.live](https://mermaid.live) o renderizar con la extensión de Mermaid de VS Code. La imagen
renderizada que se usa en la entrega está en `entrega-1-diagramas/diagrama-infraestructura.png`.

```mermaid
graph TD
    User([Usuario / Navegador Web]) -->|HTTP o HTTPS| Nginx[Nginx<br/>único punto de entrada]

    Gemini[Gemini<br/>servicio externo de Google<br/>consulta en lenguaje natural]

    subgraph VPS Oracle Cloud OCI
        Nginx ==>|todo el tráfico: servidor principal| Web1[web1<br/>réplica principal]
        Nginx -.->|backup: solo si web1 falla<br/>max_fails / fail_timeout| Web2[web2<br/>réplica en reserva]

        subgraph Datos
            DB[(MySQL 8.4)]
            KP[(Volumen compartido<br/>anillo de claves)]
        end

        subgraph Servicios
            Col[Colector de telemetría<br/>una sola instancia]
            Sim[Simulador HTTP<br/>mock inversor/sensor ARM]
            MCP[Servidor MCP<br/>SolarUY.McpServer<br/>HTTP sin estado en /mcp<br/>cinco herramientas de solo lectura]
        end

        subgraph Objetivo de las iteraciones 2 y 3
            KC[Keycloak<br/>OpenID Connect]
        end
    end

    Web1 -->|EF Core| DB
    Web2 -->|EF Core| DB
    Web1 --- KP
    Web2 --- KP
    Col -->|GET /sensores/{hw}/estado| Sim
    Col -->|EF Core: lecturas_telemetria| DB
    Web1 -.->|OIDC| KC
    Web2 -.->|OIDC| KC
    Web1 -->|HTTP, red interna: herramientas MCP| MCP
    Web2 -->|HTTP, red interna: herramientas MCP| MCP
    MCP -->|EF Core| DB
    Web1 -->|HTTPS: pregunta y herramientas| Gemini
    Web2 -->|HTTPS: pregunta y herramientas| Gemini
```

Nota: el simulador **no** escribe en la base. Es una caja negra por HTTP, así que el que consulta los
sensores y guarda las lecturas es el **colector**, que es el nodo que se agregó al diagrama original de
referencia.

El servidor MCP **no publica puerto** y no pasa por Nginx: lo alcanzan solo los contenedores de la red de
Docker, y por eso no tiene autenticación. Conoce únicamente la base de datos; la clave de Gemini vive solo en
la web, que es la que habla con el servicio externo. Como el transporte es sin estado, cualquiera de las dos
réplicas puede atender una consulta sin compartir sesión, y si el servidor MCP no responde la web sigue
funcionando: solo falla la pantalla de Consultas, que muestra su aviso.

No hay Redis ni sticky sessions: `web2` solo atiende si `web1` falla, y como las réplicas no guardan estado en
el servidor puede validar los formularios y el `TempData` de cualquier usuario.
