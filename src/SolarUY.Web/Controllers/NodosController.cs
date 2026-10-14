using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.DTOs;
using SolarUY.Application.Sensors;
using SolarUY.Application.Services;
using SolarUY.Domain.Exceptions;
using SolarUY.Web.Models;

namespace SolarUY.Web.Controllers;

/// <summary>
/// ABM de nodos. Un nodo arrastra siempre los datos de su inversor (regla de negocio 1).
/// </summary>
// TODO (etapa de seguridad): agregar [Authorize(Roles = "Administrador")] cuando esté Keycloak.
public class NodosController : Controller
{
    private readonly NodoService _nodoService;
    private readonly ParqueService _parqueService;
    private readonly PanelService _panelService;
    private readonly ISensorDisponibilidadService _sensorDisponibilidad;

    public NodosController(NodoService nodoService, ParqueService parqueService, PanelService panelService,
        ISensorDisponibilidadService sensorDisponibilidad)
    {
        _nodoService = nodoService;
        _parqueService = parqueService;
        _panelService = panelService;
        _sensorDisponibilidad = sensorDisponibilidad;
    }

    public async Task<IActionResult> Index(Guid? parqueId, CancellationToken cancellationToken)
    {
        var nodos = parqueId is null
            ? await _nodoService.ObtenerTodosAsync(cancellationToken)
            : await _nodoService.ObtenerPorParqueAsync(parqueId.Value, cancellationToken);

        return View(new NodosIndexViewModel
        {
            Nodos = nodos,
            Parques = await _parqueService.ObtenerTodosAsync(cancellationToken),
            ParqueSolarId = parqueId
        });
    }

    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancellationToken)
    {
        var nodo = await _nodoService.ObtenerPorIdAsync(id, cancellationToken);
        if (nodo is null)
        {
            return NotFound();
        }

        var paneles = await _panelService.ObtenerPorNodoAsync(id, null, cancellationToken);

        return View(new NodoDetalleViewModel
        {
            Nodo = nodo,
            Paneles = paneles
        });
    }

    public async Task<IActionResult> Crear(Guid? parqueId, CancellationToken cancellationToken)
    {
        var formulario = new NodoFormViewModel { ParqueSolarId = parqueId ?? Guid.Empty };
        await CargarParquesAsync(formulario, cancellationToken);

        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(NodoFormViewModel formulario, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await CargarParquesAsync(formulario, cancellationToken);
            return View(formulario);
        }

        var sensor = ArmarSensorRequest(formulario);
        string? nota = null;

        if (sensor is not null)
        {
            var disponibilidad = await _sensorDisponibilidad.VerificarAsync(sensor.IdentificadorHardwareARM,
                cancellationToken);

            if (disponibilidad.SePudoConsultar && !disponibilidad.Existe)
            {
                // El identificador no existe en el hardware simulado. No se crea nada: se le ofrece
                // al usuario crearlo, o seguir sin sensor.
                return await ResolverSensorInexistenteAsync(formulario, disponibilidad, cancellationToken);
            }

            if (disponibilidad.VerificacionPosible && !disponibilidad.SePudoConsultar)
            {
                nota = "No se pudo consultar el simulador, así que no se pudo verificar que el sensor exista.";
            }
        }

        return await CrearNodoAsync(formulario, sensor, cancellationToken, nota);
    }

    /// <summary>
    /// El identificador cargado no existe en el simulador. Si el usuario ya eligió qué hacer, se
    /// hace; si no, se vuelve a mostrar el formulario con las dos salidas.
    /// </summary>
    private async Task<IActionResult> ResolverSensorInexistenteAsync(NodoFormViewModel formulario,
        DisponibilidadSensor disponibilidad, CancellationToken cancellationToken)
    {
        var hardware = (formulario.IdentificadorHardwareARM ?? string.Empty).Trim();

        switch (formulario.AccionArm)
        {
            case NodoFormViewModel.AccionesArm.CrearEnHardware:
            {
                var alta = await _sensorDisponibilidad.CrearAsync(hardware, cancellationToken);

                if (!alta.Creado)
                {
                    ModelState.AddModelError(string.Empty, alta.Error ?? "No se pudo crear el sensor.");
                    await CargarParquesAsync(formulario, cancellationToken);
                    return View(formulario);
                }

                var paneles = alta.CodigosDePanel.Count > 0
                    ? $" El sensor informa los paneles {string.Join(", ", alta.CodigosDePanel)}:" +
                      " cargálos en el nodo para que empiece a recibir lecturas."
                    : string.Empty;

                return await CrearNodoAsync(formulario, ArmarSensorRequest(formulario), cancellationToken,
                    $"El simulador empezó a emular el sensor '{hardware}'." + paneles);
            }

            case NodoFormViewModel.AccionesArm.SinSensor:
                return await CrearNodoAsync(formulario, null, cancellationToken,
                    $"El nodo se creó sin sensor ARM: '{hardware}' no existe en el simulador.");

            default:
                formulario.AvisoSensorInexistente = ArmarAvisoSensorInexistente(hardware, disponibilidad);
                await CargarParquesAsync(formulario, cancellationToken);
                return View(formulario);
        }
    }

    private async Task<IActionResult> CrearNodoAsync(NodoFormViewModel formulario,
        SensorDispositivoRequest? sensor, CancellationToken cancellationToken, string? nota)
    {
        try
        {
            var nodoId = await _nodoService.CrearAsync(
                new CrearNodoRequest(
                    formulario.Nombre,
                    formulario.UbicacionDentroParque,
                    formulario.ParqueSolarId,
                    ArmarInversorRequest(formulario),
                    sensor),
                cancellationToken);

            var mensaje = $"El nodo '{formulario.Nombre}' se creó correctamente.";
            TempData["MensajeExito"] = nota is null ? mensaje : $"{mensaje} {nota}";

            return RedirectToAction(nameof(Detalle), new { id = nodoId });
        }
        catch (ReglaDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarParquesAsync(formulario, cancellationToken);
            return View(formulario);
        }
    }

    private static string ArmarAvisoSensorInexistente(string hardware, DisponibilidadSensor disponibilidad)
    {
        var existentes = disponibilidad.IdentificadoresExistentes.Count > 0
            ? string.Join(", ", disponibilidad.IdentificadoresExistentes)
            : "ninguno";

        return $"El sensor '{hardware}' no existe en el simulador. Los que existen: {existentes}.";
    }

    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        var nodo = await _nodoService.ObtenerPorIdAsync(id, cancellationToken);
        if (nodo is null)
        {
            return NotFound();
        }

        var formulario = new NodoFormViewModel
        {
            Id = nodo.Id,
            Nombre = nodo.Nombre,
            UbicacionDentroParque = nodo.UbicacionDentroParque,
            ParqueSolarId = nodo.ParqueSolarId,
            InversorModelo = nodo.Inversor.Modelo,
            InversorNumeroSerie = nodo.Inversor.NumeroSerie,
            InversorEficienciaConversion = nodo.Inversor.EficienciaConversion,
            InversorVolcadoRedActivo = nodo.Inversor.VolcadoRedActivo,
            IdentificadorHardwareARM = nodo.Inversor.Sensor?.IdentificadorHardwareARM ?? string.Empty,
            SensorVersionFirmware = nodo.Inversor.Sensor?.VersionFirmware ?? string.Empty,
            SensorUrlBase = nodo.Inversor.Sensor?.UrlBase ?? "http://localhost:5100",
            SensorActivo = nodo.Inversor.Sensor?.Activo ?? true
        };

        await CargarParquesAsync(formulario, cancellationToken);
        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, NodoFormViewModel formulario, CancellationToken cancellationToken)
    {
        if (id != formulario.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await CargarParquesAsync(formulario, cancellationToken);
            return View(formulario);
        }

        try
        {
            var request = new ActualizarNodoRequest(
                id,
                formulario.Nombre,
                formulario.UbicacionDentroParque,
                formulario.ParqueSolarId,
                ArmarInversorRequest(formulario),
                ArmarSensorRequest(formulario));

            var actualizado = await _nodoService.ActualizarAsync(request, cancellationToken);
            if (!actualizado)
            {
                return NotFound();
            }

            TempData["MensajeExito"] = $"El nodo '{formulario.Nombre}' se actualizó correctamente.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (ReglaDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarParquesAsync(formulario, cancellationToken);
            return View(formulario);
        }
    }

    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        var nodo = await _nodoService.ObtenerPorIdAsync(id, cancellationToken);
        if (nodo is null)
        {
            return NotFound();
        }

        return View(nodo);
    }

    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var eliminado = await _nodoService.EliminarAsync(id, cancellationToken);
            if (!eliminado)
            {
                return NotFound();
            }

            TempData["MensajeExito"] = "El nodo se eliminó correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (ReglaDeNegocioException ex)
        {
            TempData["MensajeError"] = ex.Message;
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }

    private static InversorRequest ArmarInversorRequest(NodoFormViewModel formulario)
        => new(
            formulario.InversorModelo,
            formulario.InversorNumeroSerie,
            formulario.InversorEficienciaConversion,
            formulario.InversorVolcadoRedActivo);

    /// <summary>
    /// Arma los datos del sensor, o <c>null</c> si el formulario no trae identificador: un nodo
    /// puede existir sin sensor, esperando que el dispositivo se conecte.
    /// </summary>
    private static SensorDispositivoRequest? ArmarSensorRequest(NodoFormViewModel formulario)
        => formulario.TieneSensor
            ? new SensorDispositivoRequest(
                formulario.IdentificadorHardwareARM!,
                formulario.SensorVersionFirmware ?? string.Empty,
                formulario.SensorUrlBase!,
                formulario.SensorActivo)
            : null;

    private async Task CargarParquesAsync(NodoFormViewModel formulario, CancellationToken cancellationToken)
        => formulario.Parques = await _parqueService.ObtenerTodosAsync(cancellationToken);
}
