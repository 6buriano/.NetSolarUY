"""Genera la presentación de la entrega (PPTX) a partir de las capturas del sistema.

Uso:
    python .entrega-build/generar-presentacion.py Documentacion/PRESENTACION-SolarUY.pptx
"""
import sys
from pathlib import Path

from PIL import Image
from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN
from pptx.util import Emu, Inches, Pt

RAIZ = Path(__file__).resolve().parent.parent
CAPTURAS = RAIZ / "entrega-1-capturas"
DIAGRAMAS = RAIZ / "entrega-1-diagramas"

AZUL = RGBColor(0x1D, 0x4E, 0xD8)
AZUL_OSCURO = RGBColor(0x0F, 0x17, 0x2A)
GRIS = RGBColor(0x64, 0x74, 0x8B)
CLARO = RGBColor(0xF1, 0xF5, 0xF9)
BLANCO = RGBColor(0xFF, 0xFF, 0xFF)

ANCHO = Inches(13.333)
ALTO = Inches(7.5)


def nueva_slide(presentacion, fondo=BLANCO):
    slide = presentacion.slides.add_slide(presentacion.slide_layouts[6])
    fondo_shape = slide.shapes.add_shape(1, 0, 0, ANCHO, ALTO)
    fondo_shape.fill.solid()
    fondo_shape.fill.fore_color.rgb = fondo
    fondo_shape.line.fill.background()
    fondo_shape.shadow.inherit = False
    return slide


def texto(slide, x, y, ancho, alto, contenido, tamano=18, bold=False, color=AZUL_OSCURO,
          alineacion=PP_ALIGN.LEFT, interlineado=1.15, espacio_parrafos=6):
    caja = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(ancho), Inches(alto))
    marco = caja.text_frame
    marco.word_wrap = True
    parrafos = contenido if isinstance(contenido, list) else [contenido]
    for indice, parrafo in enumerate(parrafos):
        p = marco.paragraphs[0] if indice == 0 else marco.add_paragraph()
        p.alignment = alineacion
        p.line_spacing = interlineado
        p.space_after = Pt(espacio_parrafos)
        run = p.add_run()
        run.text = parrafo
        run.font.size = Pt(tamano)
        run.font.bold = bold
        run.font.color.rgb = color
        run.font.name = "Segoe UI"
    return caja


def vineta(slide, x, y, ancho, alto, titulo, detalle, tamano=17):
    caja = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(ancho), Inches(alto))
    marco = caja.text_frame
    marco.word_wrap = True
    p = marco.paragraphs[0]
    p.line_spacing = 1.1
    p.space_after = Pt(2)
    r = p.add_run()
    r.text = titulo
    r.font.size = Pt(tamano)
    r.font.bold = True
    r.font.color.rgb = AZUL_OSCURO
    r.font.name = "Segoe UI"
    if detalle:
        p2 = marco.add_paragraph()
        p2.line_spacing = 1.12
        r2 = p2.add_run()
        r2.text = detalle
        r2.font.size = Pt(tamano - 3)
        r2.font.color.rgb = GRIS
        r2.font.name = "Segoe UI"
    return caja


def titulo_slide(slide, titulo, seccion):
    texto(slide, 0.6, 0.32, 8.9, 0.7, titulo, tamano=29, bold=True)
    barra = slide.shapes.add_shape(1, Inches(0.6), Inches(1.06), Inches(1.1), Inches(0.055))
    barra.fill.solid()
    barra.fill.fore_color.rgb = AZUL
    barra.line.fill.background()
    if seccion:
        texto(slide, 9.7, 0.45, 3.0, 0.4, seccion.upper(), tamano=10, color=GRIS,
              alineacion=PP_ALIGN.RIGHT)


def pie(slide, numero):
    texto(slide, 0.6, 7.03, 8, 0.3, "SolarUY · UTEC · Equipo 6", tamano=10, color=GRIS)
    texto(slide, 11.9, 7.03, 0.8, 0.3, str(numero), tamano=10, color=GRIS, alineacion=PP_ALIGN.RIGHT)


def imagen(slide, ruta, x, y, max_ancho, max_alto, borde=True):
    with Image.open(ruta) as im:
        ancho_px, alto_px = im.size
    escala = min(max_ancho / ancho_px, max_alto / alto_px)
    ancho = Inches(ancho_px * escala)
    alto = Inches(alto_px * escala)
    x_emu = Inches(x) + (Inches(max_ancho) - ancho) // 2
    if borde:
        margen = Inches(0.06)
        marco = slide.shapes.add_shape(1, x_emu - margen, Inches(y) - margen,
                                       ancho + 2 * margen, alto + 2 * margen)
        marco.fill.solid()
        marco.fill.fore_color.rgb = CLARO
        marco.line.color.rgb = RGBColor(0xE2, 0xE8, 0xF0)
        marco.line.width = Pt(0.75)
        marco.shadow.inherit = False
    slide.shapes.add_picture(str(ruta), x_emu, Inches(y), ancho, alto)
    return x_emu, ancho


def notas(slide, guion):
    slide.notes_slide.notes_text_frame.text = guion


def principal(destino: Path):
    presentacion = Presentation()
    presentacion.slide_width = ANCHO
    presentacion.slide_height = ALTO

    # ---------- 1. Portada ----------
    s = nueva_slide(presentacion, AZUL_OSCURO)
    texto(s, 0.9, 2.0, 11.5, 0.5, "TALLER DE PROGRAMACIÓN .NET 2026 · UTEC", tamano=14,
          color=RGBColor(0x93, 0xC5, 0xFD))
    texto(s, 0.9, 2.55, 11.5, 1.2, "SolarUY", tamano=66, bold=True, color=BLANCO)
    texto(s, 0.9, 3.75, 11.5, 0.6, "Gestión de parques solares y telemetría en tiempo real", tamano=26,
          color=RGBColor(0xE2, 0xE8, 0xF0))
    barra = s.shapes.add_shape(1, Inches(0.9), Inches(4.75), Inches(1.6), Inches(0.06))
    barra.fill.solid()
    barra.fill.fore_color.rgb = RGBColor(0x60, 0xA5, 0xFA)
    barra.line.fill.background()
    texto(s, 0.9, 5.0, 11.5, 0.4, "Gonzalo Buriano · Guillermina Rodriguez · Mariano Ramos · Fabricio Rivero",
          tamano=16, color=RGBColor(0xCB, 0xD5, 0xE1))
    texto(s, 0.9, 5.45, 11.5, 0.4, "github.com/6buriano/.NetSolarUY", tamano=14,
          color=RGBColor(0x93, 0xC5, 0xFD))
    notas(s, "Somos el equipo 6. SolarUY es un sistema para administrar parques solares y seguir la "
             "generación de cada panel a partir de lo que miden los sensores. En estos tres minutos "
             "mostramos la aplicación funcionando y, al final, cómo la construimos.")

    # ---------- 2. Qué hace ----------
    s = nueva_slide(presentacion)
    titulo_slide(s, "Operar el parque, no solo cargar datos", "Qué es")
    vineta(s, 0.6, 1.35, 5.5, 1.2, "Administra el parque",
           "Parques, nodos con su inversor y su sensor, y paneles con su estado de salud.")
    vineta(s, 0.6, 2.70, 5.5, 1.2, "Mide y diagnostica",
           "El colector consulta los sensores cada 5 segundos y el dominio decide el estado de cada panel.")
    vineta(s, 0.6, 4.05, 5.5, 1.2, "Opera y consulta",
           "Órdenes al hardware, mantenimiento, históricos y consultas en lenguaje natural.")
    vineta(s, 0.6, 5.40, 5.5, 1.2, "A la vista, en vivo",
           "La pantalla de inicio se actualiza sola: nadie tiene que recargar para ver qué pasa.")
    imagen(s, CAPTURAS / "07-monitoreo.png", 6.4, 1.45, 6.4, 4.9)
    pie(s, 2)
    notas(s, "Cuatro cosas: se administran los parques, se mide y se diagnostica automáticamente, se opera "
             "el parque (órdenes, mantenimiento, consultas) y todo eso se ve en vivo. A la derecha, la "
             "pantalla de inicio del sistema desplegado.")

    # ---------- 3. Arquitectura ----------
    s = nueva_slide(presentacion)
    titulo_slide(s, "Cómo está armado", "Arquitectura")
    imagen(s, DIAGRAMAS / "diagrama-infraestructura.png", 0.5, 1.45, 8.3, 5.2)
    vineta(s, 9.0, 1.5, 3.9, 1.3, "Cuatro capas",
           "Domain, Application, Infrastructure y las pantallas. Los tests de arquitectura vigilan que "
           "las dependencias apunten hacia adentro.")
    vineta(s, 9.0, 3.0, 3.9, 1.2, "Dos réplicas",
           "Nginx manda el tráfico a una y deja la otra de respaldo; comparten la base y las claves.")
    vineta(s, 9.0, 4.35, 3.9, 1.2, "El hardware es una caja negra",
           "El colector le habla por HTTP: el día que existan sensores reales, no cambia el sistema.")
    vineta(s, 9.0, 5.7, 3.9, 1.1, "Servicios aparte",
           "El servidor MCP expone cinco herramientas de solo lectura para el agente de consultas.")
    pie(s, 3)
    notas(s, "El diagrama muestra el despliegue. Lo que importa: el dominio no conoce ni la base ni las "
             "pantallas, el hardware es una caja negra por HTTP y el monitoreo viaja por SignalR.")

    # ---------- 4. Datos del parque ----------
    s = nueva_slide(presentacion)
    titulo_slide(s, "Los datos del parque: ABM completo", "Pantallas")
    imagen(s, CAPTURAS / "08-parques.png", 0.55, 1.45, 5.9, 4.0)
    imagen(s, CAPTURAS / "01-nodos.png", 6.9, 1.45, 5.9, 4.0)
    vineta(s, 0.6, 5.6, 12.1, 0.9, "Alta de nodo con un sensor que todavía no existe",
           "La pantalla avisa cuáles existen y ofrece dos salidas: crear el sensor, o cargar el nodo sin "
           "sensor y asignarlo después.")
    pie(s, 4)
    notas(s, "El ABM tiene listado con filtros, alta, edición, detalle y baja. Un nodo no puede existir sin "
             "inversor, y el sensor es opcional: si el identificador no existe, se puede crear o seguir sin él.")

    # ---------- 5. Del sensor a la pantalla ----------
    s = nueva_slide(presentacion)
    titulo_slide(s, "Del sensor a la pantalla, sin recargar", "Telemetría")
    imagen(s, CAPTURAS / "04-panel-lecturas.png", 0.55, 1.4, 6.0, 5.1)
    imagen(s, CAPTURAS / "06-panel-en-falla.png", 6.85, 1.4, 6.0, 5.1)
    texto(s, 0.6, 6.6, 12.1, 0.4,
          "Izquierda: las lecturas llegan solas, arriba de la tabla. Derecha: una sobretemperatura "
          "forzada, diagnosticada como FALLA por el dominio.", tamano=13, color=GRIS)
    pie(s, 5)
    notas(s, "El colector guarda una lectura por panel cada 5 segundos. La pantalla está suscrita: las "
             "lecturas nuevas aparecen arriba sin recargar. Cuando el panel pasa de 75 grados, el dominio lo "
             "diagnostica en falla y la alerta aparece una sola vez.")

    # ---------- 6. Mantenimiento ----------
    s = nueva_slide(presentacion)
    titulo_slide(s, "Mantenimiento: más que una marca de tiempo", "Mantenimiento")
    imagen(s, CAPTURAS / "10-mantenimiento.png", 0.55, 1.45, 6.3, 4.4)
    vineta(s, 6.9, 1.55, 5.9, 1.3, "Ciclo de vida",
           "Pendiente → en proceso → completada, o cancelada. Una completada no se reprograma.")
    vineta(s, 6.9, 3.0, 5.9, 1.7, "Al completar se registra",
           "La fecha y hora reales del trabajo, que informa el técnico, y una observación de cierre "
           "con lo que se hizo.")
    vineta(s, 6.9, 4.9, 5.9, 1.2, "Historial por panel",
           "El detalle del panel muestra todas sus tareas, con acceso a cada una.")
    pie(s, 6)
    notas(s, "Esto salió de la devolución del docente: la tarea no podía quedar como una simple marca de "
             "tiempo. Al completarla se pide la fecha real y se puede anotar qué se hizo.")

    # ---------- 7. Histórico y consultas ----------
    s = nueva_slide(presentacion)
    titulo_slide(s, "Históricos y consultas en lenguaje natural", "Consultas")
    imagen(s, CAPTURAS / "11-historico.png", 0.55, 1.45, 6.0, 4.5)
    imagen(s, CAPTURAS / "12-consultas.png", 6.9, 1.45, 6.0, 4.5)
    vineta(s, 0.6, 6.15, 6.0, 0.8, "Energía estimada",
           "Se calcula con la potencia que guarda cada lectura: sirve para comparar, no para facturar.")
    vineta(s, 6.8, 6.15, 6.0, 0.8, "Consultas con IA",
           "El agente usa cinco herramientas de solo lectura del servidor MCP; la respuesta sale de la base.")
    pie(s, 7)
    notas(s, "El histórico agrupa por día, mes o año. Y las consultas en lenguaje natural: el modelo no "
             "inventa, consulta la base a través del servidor MCP. Las herramientas son de solo lectura.")

    # ---------- 8. Cómo se hizo ----------
    s = nueva_slide(presentacion)
    titulo_slide(s, "Especificaciones antes que código", "Proceso")
    vineta(s, 0.6, 1.35, 5.9, 1.1, "Un requerimiento, tres documentos",
           "requirements (qué y criterios), specs (diseño por capa) y tasks (pasos).")
    vineta(s, 0.6, 2.55, 5.9, 1.1, "Una tarea por vez, un commit por tarea",
           "Cada tarea se compila y se prueba antes del commit; el PR incluye su carpeta de prompting.")
    vineta(s, 0.6, 3.75, 5.9, 1.1, "Verificación automática",
           "Compilación sin advertencias, tests, reglas de arquitectura y SonarQube.")
    vineta(s, 0.6, 4.95, 5.9, 1.3, "La devolución del docente como insumo",
           "Trece requerimientos documentados, más el que salió de su devolución (cierre de mantenimiento).")
    caja = s.shapes.add_shape(1, Inches(6.95), Inches(1.35), Inches(5.85), Inches(4.6))
    caja.fill.solid()
    caja.fill.fore_color.rgb = CLARO
    caja.line.fill.background()
    texto(s, 7.25, 1.6, 5.2, 0.4, "216 PRUEBAS AUTOMÁTICAS", tamano=13, color=AZUL, bold=True)
    texto(s, 7.25, 2.0, 5.2, 3.6, [
        "115 de dominio",
        "20 de aplicación (retención y monitoreo)",
        "11 de arquitectura, con ArchUnitNET",
        "70 del simulador de hardware",
    ], tamano=17, color=AZUL_OSCURO, interlineado=1.5)
    texto(s, 7.25, 4.6, 5.2, 1.2, [
        "0 advertencias de compilación",
        "0 paquetes vulnerables",
        "CI en cada push y pull request",
    ], tamano=15, color=GRIS, interlineado=1.4)
    pie(s, 8)
    notas(s, "El proceso: escribimos el requerimiento y su diseño antes del código, una tarea por vez y un "
             "commit por tarea. La verificación es automática: 216 pruebas, once reglas de arquitectura con "
             "ArchUnitNET, análisis con SonarQube y la integración continua en cada push.")

    # ---------- 9. Devolución ----------
    s = nueva_slide(presentacion)
    titulo_slide(s, "Lo que nos dejó la devolución del docente", "Mejoras")
    vineta(s, 0.6, 1.35, 6.0, 1.35, "Las aclaraciones en pantalla cansan",
           "Movimos las explicaciones a una pantalla de Ayuda; en las pantallas quedó solo lo operativo.")
    vineta(s, 6.9, 1.35, 5.9, 1.35, "El cierre del mantenimiento",
           "Fecha real del trabajo y observaciones de lo que se hizo.")
    vineta(s, 0.6, 2.95, 6.0, 1.35, "Las lecturas se actualizan solas",
           "El detalle del panel agrega las lecturas nuevas sin recargar.")
    vineta(s, 6.9, 2.95, 5.9, 1.35, "Pensar como analista, no como técnico",
           "Los textos de la Ayuda y de las pantallas están escritos para quien opera el parque.")
    imagen(s, CAPTURAS / "13-ayuda.png", 2.6, 4.55, 8.1, 2.2)
    pie(s, 9)
    notas(s, "Cuatro observaciones, cuatro cambios. La más visible es la pantalla de Ayuda: la pantalla "
             "muestra el dato y la explicación vive ahí, no repetida en cada lugar.")

    # ---------- 10. Cierre ----------
    s = nueva_slide(presentacion, AZUL_OSCURO)
    texto(s, 0.9, 1.1, 11.5, 0.8, "Cómo verlo funcionando", tamano=34, bold=True, color=BLANCO)
    texto(s, 0.9, 2.1, 11.5, 2.6, [
        "Clonar el repositorio con el submódulo: git clone --recurse-submodules",
        "Levantar todo: docker compose up -d",
        "Migrar una sola vez: docker compose --profile migracion run --rm migracion",
        "Entrar por http://localhost:8080 (Nginx → réplica principal)",
    ], tamano=19, color=RGBColor(0xE2, 0xE8, 0xF0), interlineado=1.5)
    texto(s, 0.9, 5.1, 11.5, 0.4, "github.com/6buriano/.NetSolarUY · rama main", tamano=16,
          color=RGBColor(0x93, 0xC5, 0xFD))
    texto(s, 0.9, 5.9, 11.5, 0.5, "Gracias", tamano=30, bold=True, color=BLANCO)
    notas(s, "Para verlo: clonar con el submódulo, docker compose up, migrar una vez y entrar por el puerto "
             "8080. Todo está en la rama main del repositorio. Gracias.")

    destino.parent.mkdir(parents=True, exist_ok=True)
    presentacion.save(destino)
    print(f"generado: {destino} ({len(presentacion.slides.__iter__.__self__._sldIdLst)} diapositivas)")


if __name__ == "__main__":
    principal(Path(sys.argv[1]) if len(sys.argv) > 1 else RAIZ / "Documentacion" / "PRESENTACION-SolarUY.pptx")
