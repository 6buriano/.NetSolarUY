// Recorrido guionado por la aplicación para el video de la entrega.
// El login se hace antes, sin grabar: la grabación arranca con la sesión ya iniciada.
import { chromium } from 'playwright-core';
import fs from 'node:fs';
import path from 'node:path';

const base = 'https://solaruy.fabriq.uy';
const simulador = 'http://simulador.fabriq.uy';
const salida = process.env.VIDEO_DIR;
const usuario = process.env.SOLARUY_USER;
const clave = process.env.SOLARUY_PASS;
const panelDemo = '5bae871a-0e69-4f7b-a9a2-c265c39ec0e3'; // P01 del nodo ARM-001

const chrome = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const navegador = await chromium.launch({ executablePath: chrome, headless: true });

const opciones = {
  viewport: { width: 1400, height: 900 },
  locale: 'es-UY',
  timezoneId: 'America/Montevideo',
};

const ahora = (horasAtras = 0) => {
  const d = new Date(Date.now() - horasAtras * 3600 * 1000);
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'America/Montevideo',
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', hour12: false,
  }).format(d).replace(',', '');
};

// --- 1. Login sin grabar ---
const previo = await navegador.newContext(opciones);
const paginaLogin = await previo.newPage();
await paginaLogin.goto(`${base}/Monitoreo`, { waitUntil: 'domcontentloaded' });
await paginaLogin.waitForSelector('#username');
await paginaLogin.fill('#username', usuario);
await paginaLogin.fill('#password', clave);
await paginaLogin.click('#kc-login');
await paginaLogin.waitForURL(`${base}/**`, { timeout: 90000, waitUntil: 'domcontentloaded' });
const sesion = await previo.storageState();
await previo.close();
console.log('sesión iniciada');

// --- 2. Grabación ---
const grabacion = path.join(salida, 'crudo');
fs.mkdirSync(grabacion, { recursive: true });
for (const f of fs.readdirSync(grabacion)) fs.rmSync(path.join(grabacion, f), { force: true });

const contexto = await navegador.newContext({
  ...opciones,
  storageState: sesion,
  recordVideo: { dir: grabacion, size: { width: 1400, height: 900 } },
});
contexto.setDefaultTimeout(60000);
const pagina = await contexto.newPage();
const esperar = (ms) => pagina.waitForTimeout(ms);

const taparUsuario = () =>
  pagina.evaluate(() => {
    for (const el of document.querySelectorAll('span.nav-link.text-muted')) el.textContent = 'Hola, admin';
  });

async function rotulo(texto) {
  await pagina.evaluate((t) => {
    let d = document.getElementById('dsh-rotulo');
    if (!d) {
      d = document.createElement('div');
      d.id = 'dsh-rotulo';
      d.style.cssText =
        'position:fixed;left:0;right:0;bottom:0;z-index:2147483647;background:rgba(15,23,42,.94);' +
        'color:#fff;font:500 25px/1.35 "Segoe UI",system-ui,sans-serif;padding:16px 30px;text-align:center;' +
        'letter-spacing:.2px;box-shadow:0 -6px 18px rgba(0,0,0,.18)';
      document.body.appendChild(d);
    }
    d.textContent = t;
  }, texto);
  await esperar(350);
}

async function cursor(x, y) {
  await pagina.evaluate(([px, py]) => {
    let c = document.getElementById('dsh-cursor');
    if (!c) {
      c = document.createElement('div');
      c.id = 'dsh-cursor';
      c.style.cssText =
        'position:fixed;z-index:2147483648;width:15px;height:15px;margin:-7px 0 0 -7px;border-radius:50%;' +
        'background:#dc2626;border:2px solid #fff;box-shadow:0 0 0 2px rgba(0,0,0,.3),0 2px 6px rgba(0,0,0,.25);' +
        'transition:left .28s ease,top .28s ease;pointer-events:none';
      document.body.appendChild(c);
    }
    c.style.left = `${px}px`;
    c.style.top = `${py}px`;
  }, [x, y]);
  await esperar(420);
}

async function centrar(selector) {
  const caja = await pagina.locator(selector).first().boundingBox();
  if (!caja) throw new Error('no encontre ' + selector);
  await cursor(caja.x + caja.width / 2, caja.y + caja.height / 2);
}

async function clic(selector) {
  await centrar(selector);
  await pagina.locator(selector).first().click();
  await esperar(600);
}

// Envía un formulario esperando la navegación y vuelve a tapar el usuario (el DOM es nuevo).
async function enviar(selector) {
  await centrar(selector);
  await Promise.all([
    pagina.waitForNavigation({ waitUntil: 'load', timeout: 60000 }),
    pagina.locator(selector).first().click(),
  ]);
  await esperar(900);
  await taparUsuario();
}

async function ir(ruta, texto, pausa = 6000) {
  await pagina.goto(base + ruta, { waitUntil: 'load' });
  await esperar(700);
  await taparUsuario();
  await rotulo(texto);
  await esperar(pausa);
}

let fallaForzada = false;
async function forzarFalla(activar) {
  const url = `${simulador}/simulador/fallas/ARM-001/P01`;
  const r = await fetch(activar ? `${url}?tipo=sobretemperatura` : url, { method: activar ? 'POST' : 'DELETE' });
  fallaForzada = activar;
  console.log('falla', activar ? 'forzada' : 'levantada', r.status);
}

try {
  await ir('/Monitoreo', 'Monitoreo en vivo: la página de inicio se actualiza sola cada 5 segundos', 8000);
  await ir('/Parques', 'Parques solares: alta, edición, detalle y baja', 5500);
  await ir('/Nodos', 'Nodos: cada uno con su inversor y el sensor que mide', 5500);
  await ir('/Nodos/Crear', 'Alta de nodo: el sensor es opcional; si el identificador no existe, se puede crear o seguir sin él', 6500);
  await ir('/Paneles', 'Paneles: estado de salud, filtros y acceso al detalle', 5500);

  await ir(`/Paneles/Detalle/${panelDemo}`, 'Detalle del panel: última lectura y las últimas 20, en hora de Montevideo', 4500);
  await rotulo('Las lecturas nuevas aparecen arriba sin recargar: la pantalla está suscrita al monitoreo');
  await esperar(7000);

  await forzarFalla(true);
  await rotulo('Forzamos una sobretemperatura en el sensor: el dominio diagnostica FALLA y la pantalla se actualiza sola');
  await esperar(11000);
  await forzarFalla(false);
  await rotulo('Al normalizarse, el panel vuelve a OPERATIVO en la ronda siguiente');
  await esperar(6000);

  await ir(`/Paneles/Detalle/${panelDemo}`, 'Cada panel guarda su historial de mantenimiento', 3500);
  await clic('main a[href*="/Mantenimientos/Crear"]');
  await taparUsuario();
  await rotulo('Programar una tarea: panel, tipo, fecha, descripción y responsable');
  await esperar(1800);
  await pagina.selectOption('select[name="Tipo"]', 'LIMPIEZA');
  await pagina.fill('#FechaProgramada', ahora().slice(0, 10));
  await pagina.fill('#Descripcion', 'Limpieza de los paneles del sector norte y revisión de las conexiones.');
  await pagina.fill('#PersonalAsignado', 'Cuadrilla 1');
  await esperar(2000);
  await enviar('main button.btn-primary');

  await rotulo('La tarea nace pendiente: se inicia cuando el técnico empieza');
  await enviar('main form[action*="Iniciar"] button');

  await rotulo('Al completarla se registra la fecha y hora reales del trabajo y qué se hizo');
  await esperar(1500);
  await pagina.fill('#FechaRealizada', ahora(2));
  await pagina.fill('#ObservacionesCierre', 'Se limpiaron los paneles y se revisaron las conexiones del string.');
  await esperar(2500);
  await enviar('main button.btn-success');
  await rotulo('Queda completada con la fecha real y las observaciones del cierre');
  await esperar(7000);

  await ir('/Historico', 'Histórico: generación por día, mes o año, con la energía estimada', 4000);
  await pagina.selectOption('select[name="panelId"]', { index: 0 });
  await enviar('main button.btn-primary');
  await rotulo('Cada período con su potencia promedio, máxima y la energía estimada');
  await esperar(6000);

  await ir('/Consultas', 'Consultas en lenguaje natural: el agente consulta la base con las herramientas del servidor MCP', 3000);
  await centrar('main textarea[name="Pregunta"]');
  await pagina.fill('textarea[name="Pregunta"]', '¿Cómo está funcionando el parque hoy?');
  await esperar(1500);
  await enviar('main button.btn-primary');
  await rotulo('La respuesta sale de los datos del sistema, no de la imaginación del modelo');
  await esperar(20000);

  await ir('/Ayuda', 'Las explicaciones de uso viven en Ayuda, no repetidas en cada pantalla', 6000);
  await ir('/Monitoreo', 'SolarUY · .NET 10 · Clean Architecture · MySQL · Keycloak · SignalR · MCP · 216 pruebas automáticas', 8000);

  console.log('recorrido terminado');
} finally {
  if (fallaForzada) await forzarFalla(false);
  await contexto.close();
  await navegador.close();
}
