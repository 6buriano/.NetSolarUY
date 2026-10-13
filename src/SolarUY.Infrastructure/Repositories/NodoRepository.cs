using Microsoft.EntityFrameworkCore;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Repositories;
using SolarUY.Infrastructure.Persistence;

namespace SolarUY.Infrastructure.Repositories;

public class NodoRepository : INodoRepository
{
    private readonly SolarUYDbContext _context;

    public NodoRepository(SolarUYDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Nodo>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => await ConsultaSoloLectura()
            .OrderBy(nodo => nodo.Nombre)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Nodo>> ObtenerPorParqueAsync(Guid parqueSolarId,
        CancellationToken cancellationToken = default)
        => await ConsultaSoloLectura()
            .Where(nodo => nodo.ParqueSolarId == parqueSolarId)
            .OrderBy(nodo => nodo.Nombre)
            .ToListAsync(cancellationToken);

    public async Task<Nodo?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Nodos
            .Include(nodo => nodo.Inversor)
                .ThenInclude(inversor => inversor.Sensor)
            .Include(nodo => nodo.Paneles)
            .FirstOrDefaultAsync(nodo => nodo.Id == id, cancellationToken);

    public async Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Nodos
            .AsNoTracking()
            .AnyAsync(nodo => nodo.Id == id, cancellationToken);

    public async Task<bool> ExisteIdentificadorHardwareAsync(string identificadorHardwareARM, Guid? nodoExcluidoId = null,
        CancellationToken cancellationToken = default)
    {
        var identificadorNormalizado = (identificadorHardwareARM ?? string.Empty).Trim();

        var consulta = _context.SensoresDispositivos
            .AsNoTracking()
            .Where(sensor => sensor.IdentificadorHardwareARM == identificadorNormalizado);

        if (nodoExcluidoId is not null)
        {
            var idExcluido = nodoExcluidoId.Value;

            // El sensor del propio nodo que se está editando no cuenta como duplicado.
            consulta = consulta.Where(sensor => !_context.Inversores
                .Any(inversor => inversor.Id == sensor.InversorId && inversor.NodoId == idExcluido));
        }

        return await consulta.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Trae los nodos con sensor activo, con todo lo que el colector necesita para evaluar la
    /// telemetría. La entidad queda trackeada porque el colector actualiza paneles e inversor.
    /// </summary>
    public async Task<IReadOnlyList<Nodo>> ObtenerConSensoresActivosAsync(
        CancellationToken cancellationToken = default)
        => await _context.Nodos
            .Include(nodo => nodo.Inversor)
                .ThenInclude(inversor => inversor.Sensor)
            .Include(nodo => nodo.Paneles)
            .Where(nodo => nodo.Inversor.Sensor != null && nodo.Inversor.Sensor.Activo)
            .OrderBy(nodo => nodo.Nombre)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
        CancellationToken cancellationToken = default)
        => await _context.Nodos
            .AsNoTracking()
            .Select(nodo => new { nodo.Id, nodo.Nombre })
            .ToDictionaryAsync(nodo => nodo.Id, nodo => nodo.Nombre, cancellationToken);

    public async Task<bool> ExisteNumeroSerieInversorAsync(string numeroSerie, Guid? nodoExcluidoId = null,
        CancellationToken cancellationToken = default)
    {
        var numeroSerieNormalizado = (numeroSerie ?? string.Empty).Trim();

        var consulta = _context.Inversores
            .AsNoTracking()
            .Where(inversor => inversor.NumeroSerie == numeroSerieNormalizado);

        if (nodoExcluidoId is not null)
        {
            var idExcluido = nodoExcluidoId.Value;
            consulta = consulta.Where(inversor => inversor.NodoId != idExcluido);
        }

        return await consulta.AnyAsync(cancellationToken);
    }

    public async Task AgregarAsync(Nodo nodo, CancellationToken cancellationToken = default)
    {
        // Al agregar el nodo, EF inserta también su inversor porque cuelga de la navegación.
        await _context.Nodos.AddAsync(nodo, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(Nodo nodo, CancellationToken cancellationToken = default)
    {
        // El nodo y su inversor ya están trackeados, porque se obtuvieron con ObtenerPorIdAsync.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task EliminarAsync(Nodo nodo, CancellationToken cancellationToken = default)
    {
        _context.Nodos.Remove(nodo);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Nodo> ConsultaSoloLectura()
        => _context.Nodos
            .AsNoTracking()
            .Include(nodo => nodo.Inversor)
                .ThenInclude(inversor => inversor.Sensor)
            .Include(nodo => nodo.Paneles);
}
