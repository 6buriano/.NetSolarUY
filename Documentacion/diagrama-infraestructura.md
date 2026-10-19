# Diagrama de infraestructura — SolarUY

Arquitectura **objetivo** del sistema: es la que persiguen las iteraciones 2 y 3. El despliegue actual corre
todo en Docker en un solo equipo (`docker compose up -d`): Nginx, las dos réplicas de la web, el colector,
MySQL y el simulador.

El código Mermaid de abajo es la fuente del diagrama: se puede pegar en
[mermaid.live](https://mermaid.live) o renderizar con la extensión de Mermaid de VS Code. La imagen
renderizada que se usa en la entrega está en `entrega-1-diagramas/diagrama-infraestructura.png`.

```mermaid
graph TD
    User([Usuario / Navegador Web]) -->|HTTP o HTTPS| Nginx[Nginx<br/>único punto de entrada]

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
        end

        subgraph Objetivo de las iteraciones 2 y 3
            KC[Keycloak<br/>OpenID Connect]
            MCP[Servidor MCP + Gemini]
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
    MCP -.->|herramientas sobre la base| DB
```

Nota: el simulador **no** escribe en la base. Es una caja negra por HTTP, así que el que consulta los
sensores y guarda las lecturas es el **colector**, que es el nodo que se agregó al diagrama original de
referencia. No hay Redis ni sticky sessions: `web2` solo atiende si `web1` falla, y como las réplicas no
guardan estado en el servidor puede validar los formularios y el `TempData` de cualquier usuario.
