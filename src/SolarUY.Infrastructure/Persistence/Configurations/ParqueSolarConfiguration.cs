using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarUY.Domain.Entities;

namespace SolarUY.Infrastructure.Persistence.Configurations;

public class ParqueSolarConfiguration : IEntityTypeConfiguration<ParqueSolar>
{
    public void Configure(EntityTypeBuilder<ParqueSolar> builder)
    {
        builder.ToTable("parques_solares");

        builder.HasKey(parque => parque.Id);
        builder.Property(parque => parque.Id).HasColumnName("id");

        builder.Property(parque => parque.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(parque => parque.Ubicacion)
            .HasColumnName("ubicacion")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(parque => parque.CapacidadInstaladaKW)
            .HasColumnName("capacidad_instalada_kw")
            .IsRequired();

        builder.Property(parque => parque.FechaInstalacion)
            .HasColumnName("fecha_instalacion")
            .IsRequired();

        builder.HasMany(parque => parque.Nodos)
            .WithOne()
            .HasForeignKey(nodo => nodo.ParqueSolarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(parque => parque.Nombre);
    }
}
