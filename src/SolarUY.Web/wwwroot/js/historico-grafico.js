// Gráfico de barras del histórico de generación (energía estimada por período).
// Sin librerías externas: dibuja un SVG a partir de los datos que el servidor deja en data-puntos.
(function () {
    "use strict";

    const contenedor = document.getElementById("grafico-historico");
    if (!contenedor) {
        return;
    }

    let puntos;
    try {
        puntos = JSON.parse(contenedor.dataset.puntos || "[]");
    } catch {
        puntos = [];
    }
    if (puntos.length === 0) {
        return;
    }

    const NS = "http://www.w3.org/2000/svg";
    const ancho = 900;
    const alto = 320;
    const margen = { arriba: 16, derecha: 16, abajo: 56, izquierda: 64 };
    const areaAncho = ancho - margen.izquierda - margen.derecha;
    const areaAlto = alto - margen.arriba - margen.abajo;

    const formato = new Intl.NumberFormat("es-UY", { maximumFractionDigits: 3 });

    function el(nombre, atributos, texto) {
        const nodo = document.createElementNS(NS, nombre);
        for (const [clave, valor] of Object.entries(atributos || {})) {
            nodo.setAttribute(clave, valor);
        }
        if (texto !== undefined) {
            nodo.textContent = texto;
        }
        return nodo;
    }

    // Eje Y con un máximo "redondo" para que las líneas guía queden en números legibles.
    const maximoDatos = Math.max(...puntos.map(p => p.energiaKWh), 0);
    const paso = (() => {
        if (maximoDatos <= 0) return 1;
        const bruto = maximoDatos / 4;
        const magnitud = Math.pow(10, Math.floor(Math.log10(bruto)));
        const normal = bruto / magnitud;
        const redondo = normal <= 1 ? 1 : normal <= 2 ? 2 : normal <= 5 ? 5 : 10;
        return redondo * magnitud;
    })();
    const maximo = Math.ceil(maximoDatos / paso) * paso || paso;

    const svg = el("svg", {
        viewBox: `0 0 ${ancho} ${alto}`,
        role: "img",
        "aria-label": "Energía estimada generada por período, en kWh",
        class: "w-100"
    });

    // Líneas guía y etiquetas del eje Y.
    for (let valor = 0; valor <= maximo + paso / 2; valor += paso) {
        const y = margen.arriba + areaAlto - (valor / maximo) * areaAlto;
        svg.appendChild(el("line", {
            x1: margen.izquierda, x2: ancho - margen.derecha, y1: y, y2: y,
            stroke: "currentColor", "stroke-opacity": 0.15
        }));
        svg.appendChild(el("text", {
            x: margen.izquierda - 8, y: y + 4, "text-anchor": "end", "font-size": 11, fill: "currentColor"
        }, formato.format(valor)));
    }
    svg.appendChild(el("text", {
        x: 14, y: margen.arriba + areaAlto / 2, "text-anchor": "middle", "font-size": 12, fill: "currentColor",
        transform: `rotate(-90 14 ${margen.arriba + areaAlto / 2})`
    }, "Energía estimada (kWh)"));

    // Barras. Con muchos períodos se muestra solo una de cada N etiquetas para que no se pisen.
    const anchoSlot = areaAncho / puntos.length;
    const anchoBarra = Math.max(Math.min(anchoSlot * 0.7, 48), 1);
    const cadaCuantas = Math.max(1, Math.ceil(puntos.length / 14));

    puntos.forEach((punto, i) => {
        const x = margen.izquierda + i * anchoSlot + (anchoSlot - anchoBarra) / 2;
        const altoBarra = (punto.energiaKWh / maximo) * areaAlto;
        const y = margen.arriba + areaAlto - altoBarra;

        const barra = el("rect", {
            x, y, width: anchoBarra, height: Math.max(altoBarra, 0),
            rx: 2, fill: "#198754", class: "barra-historico"
        });
        // El <title> da el tooltip nativo al pasar el mouse.
        barra.appendChild(el("title", {},
            `${punto.etiqueta}\nEnergía: ${formato.format(punto.energiaKWh)} kWh\n` +
            `Potencia máxima: ${formato.format(punto.potenciaMaximaW)} W\nLecturas: ${punto.lecturas}`));
        svg.appendChild(barra);

        if (i % cadaCuantas === 0) {
            const cx = x + anchoBarra / 2;
            const etiqueta = el("text", {
                x: cx, y: margen.arriba + areaAlto + 14, "font-size": 11, fill: "currentColor",
                "text-anchor": puntos.length > 8 ? "end" : "middle",
                transform: puntos.length > 8 ? `rotate(-35 ${cx} ${margen.arriba + areaAlto + 14})` : ""
            }, punto.etiqueta);
            svg.appendChild(etiqueta);
        }
    });

    contenedor.replaceChildren(svg);
})();
