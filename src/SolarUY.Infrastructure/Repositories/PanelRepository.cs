using Microsoft.EntityFrameworkCore;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Domain.Repositories;
using SolarUY.Infrastructure.Persistence;

namespace SolarUY.Infrastructure.Repositories;

public class PanelRepository : IPanelRepository
{
    private readonly SolarUYDbContext _context;

    public PanelRepository(SolarUYDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PanelSolar>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        => await _context.Paneles
            .AsNoTracking()
            .OrderBy(panel => panel.Fabricante)
            .ThenBy(panel => panel.Modelo)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PanelSolar>> ObtenerPorNodoAsync(Guid nodoId, EstadoSalud? estado = null,
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.Paneles
            .AsNoTracking()
            .Where(panel => panel.NodoId == nodoId);

        if (estado is not null)
        {
            var estadoBuscado = estado.Value;
            consulta = consulta.Where(panel => panel.Estado == estadoBuscado);
        }

        return await consulta
            .OrderBy(panel => panel.Modelo)
            .ToListAsync(cancellationToken);
    }

    public async Task<PanelSolar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Paneles.FirstOrDefaultAsync(panel => panel.Id == id, cancellationToken);

    public async Task<bool> ExisteCodigoEnNodoAsync(Guid nodoId, string codigoPanel, Guid? panelExcluidoId = null,
        CancellationToken cancellationToken = default)
    {
        var codigoNormalizado = (codigoPanel ?? string.Empty).Trim();

        var consulta = _context.Paneles
            .AsNoTracking()
            .Where(panel => panel.NodoId == nodoId && panel.CodigoPanel == codigoNormalizado);

        if (panelExcluidoId is not null)
        {
            var idExcluido = panelExcluidoId.Value;
            consulta = consulta.Where(panel => panel.Id != idExcluido);
        }

        return await consulta.AnyAsync(cancellationToken);
    }

    public async Task AgregarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
    {
        await _context.Paneles.AddAsync(panel, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
    {
        // El panel ya está trackeado por el contexto, porque se obtuvo con ObtenerPorIdAsync.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task EliminarAsync(PanelSolar panel, CancellationToken cancellationToken = default)
    {
        _context.Paneles.Remove(panel);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
