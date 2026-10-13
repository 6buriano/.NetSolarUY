using Microsoft.EntityFrameworkCore;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;
using SolarUY.Infrastructure.Persistence;

namespace SolarUY.Infrastructure.Repositories;

public class MantenimientoRepository : IMantenimientoRepository
{
    private readonly SolarUYDbContext _context;

    public MantenimientoRepository(SolarUYDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TareaMantenimiento>> ObtenerTodosAsync(EstadoMantenimiento? estado = null,
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.TareasMantenimiento.AsNoTracking();

        if (estado is not null)
        {
            var estadoBuscado = estado.Value;
            consulta = consulta.Where(tarea => tarea.Estado == estadoBuscado);
        }

        return await consulta
            .OrderBy(tarea => tarea.FechaProgramada)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TareaMantenimiento>> ObtenerPorPanelAsync(Guid panelSolarId,
        CancellationToken cancellationToken = default)
        => await _context.TareasMantenimiento
            .AsNoTracking()
            .Where(tarea => tarea.PanelSolarId == panelSolarId)
            .OrderByDescending(tarea => tarea.FechaProgramada)
            .ToListAsync(cancellationToken);

    public async Task<TareaMantenimiento?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.TareasMantenimiento.FirstOrDefaultAsync(tarea => tarea.Id == id, cancellationToken);

    public async Task AgregarAsync(TareaMantenimiento tarea, CancellationToken cancellationToken = default)
    {
        await _context.TareasMantenimiento.AddAsync(tarea, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(TareaMantenimiento tarea, CancellationToken cancellationToken = default)
    {
        // La tarea ya está trackeada por el contexto, porque se obtuvo con ObtenerPorIdAsync.
        await _context.SaveChangesAsync(cancellationToken);
    }
}
