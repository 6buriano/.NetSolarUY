using Microsoft.EntityFrameworkCore;
using SolarUY.Domain.Entities;

namespace SolarUY.Infrastructure.Persistence;

/// <summary>
/// Contexto de Entity Framework Core que mapea las entidades del dominio a las tablas de MySQL.
/// El mapeo de cada tabla vive en su propia clase de configuración, dentro de
/// <c>Persistence/Configurations</c>.
/// </summary>
public class SolarUYDbContext : DbContext
{
    public SolarUYDbContext(DbContextOptions<SolarUYDbContext> options) : base(options)
    {
    }

    public DbSet<ParqueSolar> ParquesSolares => Set<ParqueSolar>();

    public DbSet<Nodo> Nodos => Set<Nodo>();

    public DbSet<Inversor> Inversores => Set<Inversor>();

    public DbSet<SensorDispositivo> SensoresDispositivos => Set<SensorDispositivo>();

    public DbSet<PanelSolar> Paneles => Set<PanelSolar>();

    public DbSet<LecturaTelemetria> LecturasTelemetria => Set<LecturaTelemetria>();

    public DbSet<TareaMantenimiento> TareasMantenimiento => Set<TareaMantenimiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Toma todas las clases IEntityTypeConfiguration de este ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SolarUYDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
