using SolarUY.Application.DTOs;
using SolarUY.Application.Mappings;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Exceptions;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Services;

/// <summary>
/// Casos de uso del ABM de parques solares.
/// </summary>
public class ParqueService
{
    private readonly IParqueRepository _parqueRepository;

    public ParqueService(IParqueRepository parqueRepository)
    {
        _parqueRepository = parqueRepository;
    }

    public async Task<IReadOnlyList<ParqueDto>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        var parques = await _parqueRepository.ObtenerTodosAsync(cancellationToken);
        return parques.Select(parque => parque.ToDto()).ToList();
    }

    public async Task<ParqueDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var parque = await _parqueRepository.ObtenerPorIdAsync(id, cancellationToken);
        return parque?.ToDto();
    }

    public async Task<Guid> CrearAsync(CrearParqueRequest request, CancellationToken cancellationToken = default)
    {
        var parque = new ParqueSolar(
            request.Nombre,
            request.Ubicacion,
            request.CapacidadInstaladaKW,
            request.FechaInstalacion);

        await _parqueRepository.AgregarAsync(parque, cancellationToken);
        return parque.Id;
    }

    public async Task<bool> ActualizarAsync(ActualizarParqueRequest request, CancellationToken cancellationToken = default)
    {
        var parque = await _parqueRepository.ObtenerPorIdAsync(request.Id, cancellationToken);
        if (parque is null)
        {
            return false;
        }

        parque.Actualizar(request.Nombre, request.Ubicacion, request.CapacidadInstaladaKW, request.FechaInstalacion);
        await _parqueRepository.ActualizarAsync(parque, cancellationToken);
        return true;
    }

    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var parque = await _parqueRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (parque is null)
        {
            return false;
        }

        if (parque.TieneNodos())
        {
            throw new ReglaDeNegocioException(
                "No se puede eliminar el parque porque todavía tiene nodos asociados. Eliminá primero sus nodos.");
        }

        await _parqueRepository.EliminarAsync(parque, cancellationToken);
        return true;
    }
}
