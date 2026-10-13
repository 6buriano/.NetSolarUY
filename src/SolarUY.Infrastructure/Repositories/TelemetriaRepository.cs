using Microsoft.EntityFrameworkCore;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Repositories;
using SolarUY.Infrastructure.Persistence;

namespace SolarUY.Infrastructure.Repositories;

public class TelemetriaRepository : ITelemetriaRepository
{
    private readonly SolarUYDbContext _context;

    public TelemetriaRepository(SolarUYDbContext context)
    {
        _context = context;
    }

    public async Task AgregarAsync(LecturaTelemetria lectura, CancellationToken cancellationToken = default)
    {
        await _context.LecturasTelemetria.AddAsync(lectura, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AgregarVariasAsync(IReadOnlyList<LecturaTelemetria> lecturas,
        CancellationToken cancellationToken = default)
    {
        if (lecturas.Count == 0)
        {
            return;
        }

        await _context.LecturasTelemetria.AddRangeAsync(lecturas, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LecturaTelemetria>> ObtenerPorPanelAsync(Guid panelSolarId, DateTime desde,
        DateTime hasta, CancellationToken cancellationToken = default)
        => await _context.LecturasTelemetria
            .AsNoTracking()
            .Where(lectura => lectura.PanelSolarId == panelSolarId
                && lectura.FechaHora >= desde
                && lectura.FechaHora <= hasta)
            .OrderByDescending(lectura => lectura.FechaHora)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LecturaTelemetria>> ObtenerUltimasPorPanelAsync(Guid panelSolarId, int cantidad,
        CancellationToken cancellationToken = default)
    {
        if (cantidad <= 0)
        {
            return [];
        }

        return await _context.LecturasTelemetria
            .AsNoTracking()
            .Where(lectura => lectura.PanelSolarId == panelSolarId)
            .OrderByDescending(lectura => lectura.FechaHora)
            .Take(cantidad)
            .ToListAsync(cancellationToken);
    }
}
