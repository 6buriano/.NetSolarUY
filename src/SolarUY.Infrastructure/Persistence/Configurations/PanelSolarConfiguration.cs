using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarUY.Domain.Entities;

namespace SolarUY.Infrastructure.Persistence.Configurations;

public class PanelSolarConfiguration : IEntityTypeConfiguration<PanelSolar>
{
    public void Configure(EntityTypeBuilder<PanelSolar> builder)
    {
        builder.ToTable("paneles");

        builder.HasKey(panel => panel.Id);
        builder.Property(panel => panel.Id).HasColumnName("id");

        builder.Property(panel => panel.CodigoPanel)
            .HasColumnName("codigo_panel")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(panel => panel.Modelo)
            .HasColumnName("modelo")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(panel => panel.Fabricante)
            .HasColumnName("fabricante")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(panel => panel.PotenciaNominalW)
            .HasColumnName("potencia_nominal_w")
            .IsRequired();

        // El enum se guarda como texto para que la tabla quede legible (OPERATIVO, FALLA_DETECTADA, ...).
        builder.Property(panel => panel.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(panel => panel.Activo)
            .HasColumnName("activo")
            .IsRequired();

        builder.Property(panel => panel.FechaInstalacion)
            .HasColumnName("fecha_instalacion")
            .IsRequired();

        builder.Property(panel => panel.NodoId)
            .HasColumnName("nodo_id")
            .IsRequired();

        builder.HasMany(panel => panel.Lecturas)
            .WithOne()
            .HasForeignKey(lectura => lectura.PanelSolarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(panel => panel.Mantenimientos)
            .WithOne()
            .HasForeignKey(tarea => tarea.PanelSolarId)
            .OnDelete(DeleteBehavior.Cascade);

        // El código es el vínculo con el panel del sensor: no puede repetirse dentro del nodo.
        builder.HasIndex(panel => new { panel.NodoId, panel.CodigoPanel }).IsUnique();
    }
}
