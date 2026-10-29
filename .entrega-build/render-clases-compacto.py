"""Renderiza una versión compacta del diagrama de clases para el cuerpo del documento."""
import json
import pathlib

DIAGRAMA = """classDiagram
    class ParqueSolar {
        +string Nombre
        +double CapacidadInstaladaKW
    }
    class Nodo {
        +string Nombre
        +string UbicacionDentroParque
        +EvaluarLecturas()
    }
    class Inversor {
        +string Modelo
        +string NumeroSerie
        +bool VolcadoRedActivo
    }
    class SensorDispositivo {
        +string IdentificadorHardwareARM
        +string UrlBase
    }
    class PanelSolar {
        +string CodigoPanel
        +double PotenciaNominalW
        +EstadoSalud Estado
    }
    class LecturaTelemetria {
        +DateTime FechaHora
        +double PotenciaGeneradaWatts
        +EstadoSalud EstadoDetectado
    }
    class TareaMantenimiento {
        +TipoMantenimiento Tipo
        +EstadoMantenimiento Estado
    }
    ParqueSolar "1" *-- "1..*" Nodo : contiene
    Nodo "1" *-- "1" Inversor : equipado con
    Inversor "1" *-- "0..1" SensorDispositivo : acoplado con
    Nodo "1" *-- "1..*" PanelSolar : agrupa
    PanelSolar "1" --> "0..*" LecturaTelemetria : registra
    PanelSolar "1" o-- "0..*" TareaMantenimiento : requiere
"""

html = """<!DOCTYPE html>
<html lang="es"><head><meta charset="utf-8">
<style>
  body { margin: 0; padding: 24px; background: #fff; font-family: 'Segoe UI', sans-serif; }
  #error { color: #b00020; font-family: Consolas, monospace; font-size: 18px; white-space: pre-wrap; }
  .mermaid { font-size: 26px; }
</style>
<script src="https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js"></script>
</head><body>
<div id="cont"></div><pre id="error"></pre>
<script>
const texto = __DIAGRAMA__;
(async () => {
  mermaid.initialize({ startOnLoad: false, theme: "default", class: { useMaxWidth: false } });
  try {
    const { svg } = await mermaid.render("clases", texto);
    document.getElementById("cont").innerHTML = svg;
  } catch (e) {
    document.getElementById("error").textContent = "ERROR: " + String(e && e.message ? e.message : e);
  }
})();
</script>
</body></html>"""

html = html.replace("__DIAGRAMA__", json.dumps(DIAGRAMA))
pathlib.Path(".entrega-build/diagrama-clases-compacto.html").write_text(html, encoding="utf-8")
print("html compacto generado")
