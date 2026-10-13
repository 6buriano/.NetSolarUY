using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarUY.Domain.Entities;

namespace SolarUY.Infrastructure.Persistence.Configurations;

public class TareaMantenimientoConfiguration : IEntityTypeConfiguration<TareaMantenimiento>
{
    public void Configure(EntityTypeBuilder<TareaMantenimiento> builder)
    {
        builder.ToTable("mantenimientos");

        builder.HasKey(tarea => tarea.Id);
        builder.Property(tarea => tarea.Id).HasColumnName("id");

        builder.Property(tarea => tarea.PanelSolarId)
            .HasColumnName("panel_solar_id")
            .IsRequired();

        builder.Property(tarea => tarea.Tipo)
            .HasColumnName("tipo")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(tarea => tarea.FechaProgramada)
            .HasColumnName("fecha_programada")
            .IsRequired();

        builder.Property(tarea => tarea.FechaRealizada)
            .HasColumnName("fecha_realizada");

        builder.Property(tarea => tarea.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(tarea => tarea.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(tarea => tarea.PersonalAsignado)
            .HasColumnName("personal_asignado")
            .HasMaxLength(150);
    }
}
