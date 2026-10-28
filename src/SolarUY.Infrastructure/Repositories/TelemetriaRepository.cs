using Microsoft.EntityFrameworkCore;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
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

    public async Task<IReadOnlyList<AgregadoPotencia>> ObtenerAgregadoPorPanelAsync(Guid panelSolarId,
        GranularidadConsulta granularidad, DateTime? desde = null, DateTime? hasta = null,
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.LecturasTelemetria
            .AsNoTracking()
            .Where(lectura => lectura.PanelSolarId == panelSolarId);

        if (desde is not null)
        {
            consulta = consulta.Where(lectura => lectura.FechaHora >= desde.Value);
        }

        if (hasta is not null)
        {
            consulta = consulta.Where(lectura => lectura.FechaHora <= hasta.Value);
        }

        // Se agrupa por año/mes/día como enteros (bien soportado por el proveedor) y recién
        // después se arma el DateTime del período, ya del lado del cliente: construir el DateTime
        // dentro de la consulta no siempre se puede traducir a SQL.
        switch (granularidad)
        {
            case GranularidadConsulta.DIA:
            {
                var porDia = await consulta
                    .GroupBy(lectura => new { lectura.FechaHora.Year, lectura.FechaHora.Month, lectura.FechaHora.Day })
                    .Select(grupo => new
                    {
                        grupo.Key.Year,
                        grupo.Key.Month,
                        grupo.Key.Day,
                        Cantidad = grupo.Count(),
                        Promedio = grupo.Average(lectura => lectura.PotenciaGeneradaWatts),
                        Maximo = grupo.Max(lectura => lectura.PotenciaGeneradaWatts),
                        Suma = grupo.Sum(lectura => lectura.PotenciaGeneradaWatts)
                    })
                    .ToListAsync(cancellationToken);

                return porDia
                    .Select(grupo => new AgregadoPotencia(new DateTime(grupo.Year, grupo.Month, grupo.Day),
                        grupo.Cantidad, grupo.Promedio, grupo.Maximo, grupo.Suma))
                    .OrderByDescending(agregado => agregado.PeriodoInicio)
                    .ToList();
            }

            case GranularidadConsulta.MES:
            {
                var porMes = await consulta
                    .GroupBy(lectura => new { lectura.FechaHora.Year, lectura.FechaHora.Month })
                    .Select(grupo => new
                    {
                        grupo.Key.Year,
                        grupo.Key.Month,
                        Cantidad = grupo.Count(),
                        Promedio = grupo.Average(lectura => lectura.PotenciaGeneradaWatts),
                        Maximo = grupo.Max(lectura => lectura.PotenciaGeneradaWatts),
                        Suma = grupo.Sum(lectura => lectura.PotenciaGeneradaWatts)
                    })
                    .ToListAsync(cancellationToken);

                return porMes
                    .Select(grupo => new AgregadoPotencia(new DateTime(grupo.Year, grupo.Month, 1),
                        grupo.Cantidad, grupo.Promedio, grupo.Maximo, grupo.Suma))
                    .OrderByDescending(agregado => agregado.PeriodoInicio)
                    .ToList();
            }

            default: // ANIO
            {
                var porAnio = await consulta
                    .GroupBy(lectura => lectura.FechaHora.Year)
                    .Select(grupo => new
                    {
                        Year = grupo.Key,
                        Cantidad = grupo.Count(),
                        Promedio = grupo.Average(lectura => lectura.PotenciaGeneradaWatts),
                        Maximo = grupo.Max(lectura => lectura.PotenciaGeneradaWatts),
                        Suma = grupo.Sum(lectura => lectura.PotenciaGeneradaWatts)
                    })
                    .ToListAsync(cancellationToken);

                return porAnio
                    .Select(grupo => new AgregadoPotencia(new DateTime(grupo.Year, 1, 1),
                        grupo.Cantidad, grupo.Promedio, grupo.Maximo, grupo.Suma))
                    .OrderByDescending(agregado => agregado.PeriodoInicio)
                    .ToList();
            }
        }
    }
}
