using SolarUY.Application.DTOs;
using SolarUY.Application.Mappings;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Exceptions;
using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Services;

/// <summary>
/// Casos de uso del ABM de nodos. Un nodo se crea siempre junto con su inversor,
/// porque la regla de negocio 1 no admite un nodo sin inversor.
/// </summary>
public class NodoService
{
    private readonly INodoRepository _nodoRepository;
    private readonly IParqueRepository _parqueRepository;

    public NodoService(INodoRepository nodoRepository, IParqueRepository parqueRepository)
    {
        _nodoRepository = nodoRepository;
        _parqueRepository = parqueRepository;
    }

    public async Task<IReadOnlyList<NodoDto>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        var nodos = await _nodoRepository.ObtenerTodosAsync(cancellationToken);
        var nombresParques = await _parqueRepository.ObtenerNombresAsync(cancellationToken);

        return nodos
            .Select(nodo => nodo.ToDto(nombresParques.GetValueOrDefault(nodo.ParqueSolarId)))
            .ToList();
    }

    public async Task<IReadOnlyList<NodoDto>> ObtenerPorParqueAsync(Guid parqueSolarId,
        CancellationToken cancellationToken = default)
    {
        var nodos = await _nodoRepository.ObtenerPorParqueAsync(parqueSolarId, cancellationToken);
        var nombresParques = await _parqueRepository.ObtenerNombresAsync(cancellationToken);
        var nombreParque = nombresParques.GetValueOrDefault(parqueSolarId);

        return nodos.Select(nodo => nodo.ToDto(nombreParque)).ToList();
    }

    public async Task<NodoDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var nodo = await _nodoRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (nodo is null)
        {
            return null;
        }

        var nombresParques = await _parqueRepository.ObtenerNombresAsync(cancellationToken);
        return nodo.ToDto(nombresParques.GetValueOrDefault(nodo.ParqueSolarId));
    }

    public async Task<Guid> CrearAsync(CrearNodoRequest request, CancellationToken cancellationToken = default)
    {
        await ValidarParqueExisteAsync(request.ParqueSolarId, cancellationToken);
        await ValidarNumeroSerieLibreAsync(request.Inversor.NumeroSerie, null, cancellationToken);
        await ValidarIdentificadorHardwareLibreAsync(request.Sensor.IdentificadorHardwareARM, null, cancellationToken);

        var inversor = new Inversor(
            request.Inversor.Modelo,
            request.Inversor.NumeroSerie,
            request.Inversor.EficienciaConversion,
            request.Inversor.VolcadoRedActivo);

        var nodo = new Nodo(request.Nombre, request.UbicacionDentroParque, request.ParqueSolarId, inversor);

        // El sensor ARM completa la dupla del nodo: es a esa dirección a la que el colector consulta.
        nodo.Inversor.RegistrarSensor(ArmarSensor(request.Sensor));

        await _nodoRepository.AgregarAsync(nodo, cancellationToken);
        return nodo.Id;
    }

    public async Task<bool> ActualizarAsync(ActualizarNodoRequest request, CancellationToken cancellationToken = default)
    {
        var nodo = await _nodoRepository.ObtenerPorIdAsync(request.Id, cancellationToken);
        if (nodo is null)
        {
            return false;
        }

        await ValidarParqueExisteAsync(request.ParqueSolarId, cancellationToken);
        await ValidarNumeroSerieLibreAsync(request.Inversor.NumeroSerie, request.Id, cancellationToken);
        await ValidarIdentificadorHardwareLibreAsync(request.Sensor.IdentificadorHardwareARM, request.Id, cancellationToken);

        nodo.Actualizar(request.Nombre, request.UbicacionDentroParque, request.ParqueSolarId);
        nodo.Inversor.Actualizar(
            request.Inversor.Modelo,
            request.Inversor.NumeroSerie,
            request.Inversor.EficienciaConversion,
            request.Inversor.VolcadoRedActivo);

        // Si el nodo todavía no tenía sensor se registra; si ya tenía, se actualiza el mismo
        // (la relación es 1 a 1 y el sensor no se puede reemplazar por otro).
        if (nodo.Inversor.Sensor is null)
        {
            nodo.Inversor.RegistrarSensor(ArmarSensor(request.Sensor));
        }
        else
        {
            nodo.Inversor.Sensor.Actualizar(
                request.Sensor.IdentificadorHardwareARM,
                request.Sensor.VersionFirmware,
                request.Sensor.Activo,
                request.Sensor.UrlBase);
        }

        await _nodoRepository.ActualizarAsync(nodo, cancellationToken);
        return true;
    }

    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var nodo = await _nodoRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (nodo is null)
        {
            return false;
        }

        if (nodo.TienePaneles())
        {
            throw new ReglaDeNegocioException(
                "No se puede eliminar el nodo porque todavía tiene paneles asociados. Eliminá primero sus paneles.");
        }

        await _nodoRepository.EliminarAsync(nodo, cancellationToken);
        return true;
    }

    private async Task ValidarParqueExisteAsync(Guid parqueSolarId, CancellationToken cancellationToken)
    {
        var existe = await _parqueRepository.ExisteAsync(parqueSolarId, cancellationToken);
        if (!existe)
        {
            throw new ReglaDeNegocioException("El parque solar seleccionado no existe.");
        }
    }

    private async Task ValidarNumeroSerieLibreAsync(string numeroSerie, Guid? nodoExcluidoId,
        CancellationToken cancellationToken)
    {
        var yaExiste = await _nodoRepository.ExisteNumeroSerieInversorAsync(numeroSerie, nodoExcluidoId, cancellationToken);
        if (yaExiste)
        {
            throw new ReglaDeNegocioException(
                $"Ya hay un inversor registrado con el número de serie '{numeroSerie}'. El número de serie es único.");
        }
    }

    private async Task ValidarIdentificadorHardwareLibreAsync(string identificadorHardwareARM, Guid? nodoExcluidoId,
        CancellationToken cancellationToken)
    {
        var yaExiste = await _nodoRepository.ExisteIdentificadorHardwareAsync(identificadorHardwareARM, nodoExcluidoId,
            cancellationToken);

        if (yaExiste)
        {
            throw new ReglaDeNegocioException(
                $"Ya hay un sensor registrado con el identificador '{identificadorHardwareARM}'. El identificador de hardware es único.");
        }
    }

    private static SensorDispositivo ArmarSensor(SensorDispositivoRequest request)
        => new(request.IdentificadorHardwareARM, request.VersionFirmware, request.Activo, request.UrlBase);
}
