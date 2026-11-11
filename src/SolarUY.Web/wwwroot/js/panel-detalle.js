// Detalle del panel en vivo (req012, criterio 6): cada lectura nueva que guarda el colector aparece
// arriba de la tabla, sin recargar, y se actualiza el bloque de "Última lectura".
(function () {
    "use strict";

    const cuerpo = document.getElementById("lecturas-recientes");
    if (!cuerpo) {
        return; // El panel todavía no tiene lecturas: no hay tabla que actualizar.
    }

    const panelId = cuerpo.dataset.panelId;
    const ultima = document.getElementById("ultima-lectura");
    const cantidadFilas = cuerpo.rows.length || 20; // Se mantiene la misma cantidad que trajo el servidor.

        // Mismo formato que el servidor (punto decimal), para que no se mezclen "0,47" y "0.48".
    const formatoNumero = new Intl.NumberFormat("en-US", { maximumFractionDigits: 2, useGrouping: false });
    const formatoFecha = new Intl.DateTimeFormat("es-UY", {
        timeZone: "America/Montevideo", day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false
    });

    const clasesEstado = {
        OPERATIVO: "text-bg-success",
        RENDIMIENTO_BAJO: "text-bg-warning",
        FALLA_DETECTADA: "text-bg-danger",
        DESCONECTADO: "text-bg-secondary"
    };
    const clasesFila = { FALLA_DETECTADA: "table-danger", RENDIMIENTO_BAJO: "table-warning" };

    // La base guarda UTC pero la fecha llega sin la "Z": se la agregamos para que el navegador no
    // la tome como hora local.
    function fechaUtc(texto) {
        return new Date(/Z$|[+-]\d\d:\d\d$/.test(texto) ? texto : texto + "Z");
    }

    function textoFecha(texto) {
        return formatoFecha.format(fechaUtc(texto)).replace(",", "");
    }

    function crearBadge(estado) {
        const badge = document.createElement("span");
        badge.className = "badge " + (clasesEstado[estado] ?? "text-bg-secondary");
        badge.textContent = estado;
        return badge;
    }

    function crearCelda(contenido, alineadaDerecha) {
        const celda = document.createElement("td");
        if (alineadaDerecha) {
            celda.className = "text-end";
        }
        if (typeof contenido === "string") {
            celda.textContent = contenido;
        } else {
            celda.appendChild(contenido);
        }
        return celda;
    }

    function agregarLectura(lectura) {
    // Si la lectura ya está en la tabla (llegó con la página y también por SignalR), no se repite.
                if (cuerpo.querySelector(`[data-lectura-id="${lectura.id}"]`)) {
                    return;
                }
        const fila = document.createElement("tr");
        fila.className = clasesFila[lectura.estadoDetectado] ?? "";
        fila.dataset.lecturaId = lectura.id;
        fila.append(
            crearCelda(textoFecha(lectura.fechaHora)),
            crearCelda(formatoNumero.format(lectura.corrienteContinua), true),
            crearCelda(formatoNumero.format(lectura.voltaje), true),
            crearCelda(formatoNumero.format(lectura.potenciaGeneradaWatts), true),
            crearCelda(formatoNumero.format(lectura.temperaturaCelsius), true),
            crearCelda(crearBadge(lectura.estadoDetectado))
        );

        cuerpo.prepend(fila); // La más nueva va arriba...
        while (cuerpo.rows.length > cantidadFilas) {
            cuerpo.lastElementChild.remove(); // ...y la más vieja sale.
        }

        if (ultima) {
            ultima.querySelector('[data-campo="fecha"]').textContent = textoFecha(lectura.fechaHora);
            ultima.querySelector('[data-campo="corriente"]').textContent = formatoNumero.format(lectura.corrienteContinua);
            ultima.querySelector('[data-campo="voltaje"]').textContent = formatoNumero.format(lectura.voltaje);
            ultima.querySelector('[data-campo="potencia"]').textContent = formatoNumero.format(lectura.potenciaGeneradaWatts);
            ultima.querySelector('[data-campo="temperatura"]').textContent = formatoNumero.format(lectura.temperaturaCelsius);
            ultima.querySelector('[data-campo="estado"]').replaceWith(crearBadgeConCampo(lectura.estadoDetectado));
        }
    }


    function crearBadgeConCampo(estado) {
        const badge = crearBadge(estado);
        badge.dataset.campo = "estado";
        return badge;
    }

    const conexion = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/monitoreo")
        .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: contexto => (contexto.previousRetryCount === 0 ? 0 : 5000)
        })
        .build();

    conexion.on("lectura", agregarLectura);

    // Los grupos son por conexión: al reconectar (por ejemplo, después de un failover) hay que
    // volver a suscribirse a este panel.
    conexion.onreconnected(() => conexion.invoke("SuscribirsePanel", panelId));

    async function iniciar() {
        try {
            await conexion.start();
            await conexion.invoke("SuscribirsePanel", panelId);
        } catch (error) {
            console.error(error);
            setTimeout(iniciar, 5000);
        }
    }

    iniciar();
})();