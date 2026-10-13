using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarUY.Domain.Entities;

namespace SolarUY.Infrastructure.Persistence.Configurations;

public class InversorConfiguration : IEntityTypeConfiguration<Inversor>
{
    public void Configure(EntityTypeBuilder<Inversor> builder)
    {
        builder.ToTable("inversores");

        builder.HasKey(inversor => inversor.Id);
        builder.Property(inversor => inversor.Id).HasColumnName("id");

        builder.Property(inversor => inversor.Modelo)
            .HasColumnName("modelo")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(inversor => inversor.NumeroSerie)
            .HasColumnName("numero_serie")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(inversor => inversor.EficienciaConversion)
            .HasColumnName("eficiencia_conversion")
            .IsRequired();

        builder.Property(inversor => inversor.VolcadoRedActivo)
            .HasColumnName("volcado_red_activo")
            .IsRequired();

        builder.Property(inversor => inversor.NodoId)
            .HasColumnName("nodo_id")
            .IsRequired();

        builder.HasOne(inversor => inversor.Sensor)
            .WithOne()
            .HasForeignKey<SensorDispositivo>(sensor => sensor.InversorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Un número de serie identifica físicamente a un inversor: no puede repetirse.
        builder.HasIndex(inversor => inversor.NumeroSerie).IsUnique();
    }
}
