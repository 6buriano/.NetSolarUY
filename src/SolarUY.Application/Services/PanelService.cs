using SolarUY.Application.DTOs;
using SolarUY.Application.Mappings;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Services;

/// <summary>
/// Casos de uso del ABM de paneles solares.
/// </summary>
public class PanelService
{
    private readonly IPanelRepository _panelRepository;
    private readonly INodoRepository _nodoRepository;
    private readonly ITelemetriaRepository _telemetriaRepository;

    public PanelService(IPanelRepository panelRepository, INodoRepository nodoRepository,
        ITelemetriaRepository telemetriaRepository)
    {
        _panelRepository = panelRepository;
        _nodoRepository = nodoRepository;
        _telemetriaRepository = telemetriaRepository;
    }

    public async Task<IReadOnlyList<PanelDto>> ObtenerTodosAsync(EstadoSalud? estado = null,
        CancellationToken cancellationToken = default)
    {
        var paneles = await _panelRepository.ObtenerTodosAsync(cancellationToken);
        var nombresNodos = await _nodoRepository.ObtenerNombresAsync(cancellationToken);

        return paneles
            .Where(panel => estado is null || panel.Estado == estado)
            .Select(panel => panel.ToDto(nombresNodos.GetValueOrDefault(panel.NodoId)))
            .ToList();
    }

    public async Task<IReadOnlyList<PanelDto>> ObtenerPorNodoAsync(Guid nodoId, EstadoSalud? estado = null,
        CancellationToken cancellationToken = default)
    {
        var paneles = await _panelRepository.ObtenerPorNodoAsync(nodoId, estado, cancellationToken);
        var nombresNodos = await _nodoRepository.ObtenerNombresAsync(cancellationToken);
        var nombreNodo = nombresNodos.GetValueOrDefault(nodoId);

        return paneles.Select(panel => panel.ToDto(nombreNodo)).ToList();
    }

    public async Task<PanelDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var panel = await _panelRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (panel is null)
        {
            return null;
        }

        var nombresNodos = await _nodoRepository.ObtenerNombresAsync(cancellationToken);
        return panel.ToDto(nombresNodos.GetValueOrDefault(panel.NodoId));
    }

    /// <summary>Última lectura de telemetría del panel, o <c>null</c> si el simulador todavía no envió ninguna.</summary>
    public async Task<LecturaTelemetriaDto?> ObtenerUltimaLecturaAsync(Guid panelId,
        CancellationToken cancellationToken = default)
    {
        var lectura = await _telemetriaRepository.ObtenerUltimaPorPanelAsync(panelId, cancellationToken);
        return lectura?.ToDto();
    }

    public async Task<Guid> CrearAsync(CrearPanelRequest request, CancellationToken cancellationToken = default)
    {
        await ValidarNodoExisteAsync(request.NodoId, cancellationToken);
        await ValidarCodigoLibreEnElNodoAsync(request.NodoId, request.CodigoPanel, null, cancellationToken);

        var panel = new PanelSolar(
            request.CodigoPanel,
            request.Modelo,
            request.Fabricante,
            request.PotenciaNominalW,
            request.Estado,
            request.FechaInstalacion,
            request.NodoId,
            request.Activo);

        await _panelRepository.AgregarAsync(panel, cancellationToken);
        return panel.Id;
    }

    public async Task<bool> ActualizarAsync(ActualizarPanelRequest request, CancellationToken cancellationToken = default)
    {
        var panel = await _panelRepository.ObtenerPorIdAsync(request.Id, cancellationToken);
        if (panel is null)
        {
            return false;
        }

        await ValidarNodoExisteAsync(request.NodoId, cancellationToken);
        await ValidarCodigoLibreEnElNodoAsync(request.NodoId, request.CodigoPanel, request.Id, cancellationToken);

        panel.Actualizar(
            request.CodigoPanel,
            request.Modelo,
            request.Fabricante,
            request.PotenciaNominalW,
            request.Estado,
            request.FechaInstalacion,
            request.NodoId,
            request.Activo);

        await _panelRepository.ActualizarAsync(panel, cancellationToken);
        return true;
    }

    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var panel = await _panelRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (panel is null)
        {
            return false;
        }

        // La telemetría y el mantenimiento del panel se eliminan en cascada, porque cuelgan de él.
        await _panelRepository.EliminarAsync(panel, cancellationToken);
        return true;
    }

    private async Task ValidarNodoExisteAsync(Guid nodoId, CancellationToken cancellationToken)
    {
        var existe = await _nodoRepository.ExisteAsync(nodoId, cancellationToken);
        if (!existe)
        {
            throw new ReglaDeNegocioException("El nodo seleccionado no existe.");
        }
    }

    private async Task ValidarCodigoLibreEnElNodoAsync(Guid nodoId, string codigoPanel, Guid? panelExcluidoId,
        CancellationToken cancellationToken)
    {
        var yaExiste = await _panelRepository.ExisteCodigoEnNodoAsync(nodoId, codigoPanel, panelExcluidoId, cancellationToken);
        if (yaExiste)
        {
            throw new ReglaDeNegocioException(
                $"Ya hay un panel con el código '{codigoPanel}' en este nodo. El código identifica al panel ante el sensor y no puede repetirse.");
        }
    }
}
