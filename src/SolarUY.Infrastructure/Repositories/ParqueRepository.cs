using Microsoft.EntityFrameworkCore;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Repositories;
using SolarUY.Infrastructure.Persistence;

namespace SolarUY.Infrastructure.Repositories;

public class ParqueRepository : IParqueRepository
{
    private readonly SolarUYDbContext _context;

    public ParqueRepository(SolarUYDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ParqueSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => await _context.ParquesSolares
            .AsNoTracking()
            .Include(parque => parque.Nodos)
                .ThenInclude(nodo => nodo.Paneles)
            .OrderBy(parque => parque.Nombre)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Trae el parque con sus nodos y paneles. Los paneles son necesarios para calcular los
    /// totales del DTO, y los nodos para saber si el parque se puede eliminar. La entidad
    /// queda trackeada para poder modificarla después.
    /// </summary>
    public async Task<ParqueSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.ParquesSolares
            .Include(parque => parque.Nodos)
                .ThenInclude(nodo => nodo.Paneles)
            .FirstOrDefaultAsync(parque => parque.Id == id, cancellationToken);

    public async Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.ParquesSolares
            .AsNoTracking()
            .AnyAsync(parque => parque.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
        CancellationToken cancellationToken = default)
        => await _context.ParquesSolares
            .AsNoTracking()
            .Select(parque => new { parque.Id, parque.Nombre })
            .ToDictionaryAsync(parque => parque.Id, parque => parque.Nombre, cancellationToken);

    public async Task AgregarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
    {
        await _context.ParquesSolares.AddAsync(parque, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
    {
        // El parque ya está trackeado por el contexto, porque se obtuvo con ObtenerPorIdAsync.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task EliminarAsync(ParqueSolar parque, CancellationToken cancellationToken = default)
    {
        _context.ParquesSolares.Remove(parque);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
