// Monitoreo en tiempo real (req012): recibe por SignalR la foto del parque y los avisos de falla,
// y actualiza la pantalla en el lugar, sin recargar.
(function () {
    "use strict";

    const contenedor = document.getElementById("monitoreo");
    if (!contenedor) {
        return; // Sin parques cargados no hay nada que actualizar.
    }

    const indicador = document.getElementById("estado-conexion");
    const horaActualizacion = document.getElementById("ultima-actualizacion");
    const parqueId = contenedor.dataset.parqueId;

    // Mismo formato que el servidor (punto decimal), para que no cambie al actualizarse.
    const formatoEntero = new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 });
    const formatoDecimal = new Intl.NumberFormat("en-US", { minimumFractionDigits: 1, maximumFractionDigits: 1 });
    const formatoHora = new Intl.DateTimeFormat("es-UY", {
        timeZone: "America/Montevideo", hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false
    });

    // Los mismos colores que EstadoSaludHelper.ClaseBadge en C#.
    const clasesEstado = {
        OPERATIVO: "text-bg-success",
        RENDIMIENTO_BAJO: "text-bg-warning",
        FALLA_DETECTADA: "text-bg-danger",
        DESCONECTADO: "text-bg-secondary"
    };

    function mostrarConexion(texto, clase) {
        indicador.textContent = texto;
        indicador.className = "badge " + clase;
    }

    function textoONada(valor, formato) {
        return valor === null || valor === undefined ? "—" : formato.format(valor);
    }

    function actualizar(instantanea) {
        const parque = instantanea.parques.find(p => p.parqueId === parqueId);
        if (!parque) {
            return;
        }

        contenedor.querySelector("[data-potencia-parque]").textContent = formatoEntero.format(parque.potenciaActualW);

        for (const nodo of parque.nodos) {
            const tarjeta = contenedor.querySelector(`[data-nodo-id="${nodo.nodoId}"]`);
            if (!tarjeta) {
                continue; // Nodo creado después de abrir la página: aparece al recargar.
            }
            tarjeta.querySelector("[data-potencia-nodo]").textContent = formatoEntero.format(nodo.potenciaActualW);

            for (const panel of nodo.paneles) {
                const fila = tarjeta.querySelector(`[data-panel-id="${panel.panelId}"]`);
                if (!fila) {
                    continue;
                }
                fila.querySelector('[data-campo="potencia"]').textContent = textoONada(panel.potenciaActualW, formatoEntero);
                fila.querySelector('[data-campo="temperatura"]').textContent = textoONada(panel.temperaturaCelsius, formatoDecimal);

                const badge = fila.querySelector('[data-campo="estado"]');
                badge.textContent = panel.estado;
                badge.className = "badge " + (clasesEstado[panel.estado] ?? "text-bg-secondary");

                if (panel.estado !== "FALLA_DETECTADA") {
                    fila.classList.remove("table-danger");
                }
            }
        }

        horaActualizacion.textContent = formatoHora.format(new Date(instantanea.generadaUtc));
        contenedor.classList.remove("opacity-50");
    }

    function avisarFalla(falla) {
        if (falla.parqueId !== parqueId) {
            return; // La falla es de otro parque.
        }

        const fila = contenedor.querySelector(`[data-panel-id="${falla.panelId}"]`);
        if (fila) {
            fila.classList.add("table-danger");
        }

        const toast = document.createElement("div");
        toast.className = "toast text-bg-danger border-0";
        toast.setAttribute("role", "alert");
        toast.innerHTML =
            '<div class="d-flex"><div class="toast-body"></div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Cerrar"></button></div>';
        // textContent y no innerHTML: el código del panel lo escribe un usuario.
        toast.querySelector(".toast-body").textContent = `Falla en el panel ${falla.codigoPanel} (${falla.nodoNombre}).`;

        document.getElementById("avisos").appendChild(toast);
        toast.addEventListener("hidden.bs.toast", () => toast.remove());
        new bootstrap.Toast(toast, { delay: 10000 }).show();
    }

    const conexion = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/monitoreo")
        // Reintenta siempre: al instante la primera vez y después cada 5 s (por ejemplo, mientras
        // nginx pasa de web1 a web2).
        .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: contexto => (contexto.previousRetryCount === 0 ? 0 : 5000)
        })
        .build();

    conexion.on("instantanea", actualizar);
    conexion.on("falla", avisarFalla);

    conexion.onreconnecting(() => {
        mostrarConexion("Reconectando…", "text-bg-warning");
        contenedor.classList.add("opacity-50"); // Los datos en pantalla ya no están al día.
    });

    conexion.onreconnected(async () => {
        // Los grupos son por conexión: al reconectar hay que volver a suscribirse.
        await conexion.invoke("SuscribirseMonitoreo");
        mostrarConexion("En vivo", "text-bg-success");
    });

    conexion.onclose(() => {
        mostrarConexion("Desconectado", "text-bg-danger");
        contenedor.classList.add("opacity-50");
    });

    async function iniciar() {
        try {
            await conexion.start();
            await conexion.invoke("SuscribirseMonitoreo");
            mostrarConexion("En vivo", "text-bg-success");
        } catch (error) {
            console.error(error);
            mostrarConexion("Desconectado", "text-bg-danger");
            setTimeout(iniciar, 5000); // El primer intento no tiene reconexión automática.
        }
    }

    iniciar();
})();