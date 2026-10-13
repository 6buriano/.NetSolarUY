using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarUY.Domain.Entities;

namespace SolarUY.Infrastructure.Persistence.Configurations;

public class NodoConfiguration : IEntityTypeConfiguration<Nodo>
{
    public void Configure(EntityTypeBuilder<Nodo> builder)
    {
        builder.ToTable("nodos");

        builder.HasKey(nodo => nodo.Id);
        builder.Property(nodo => nodo.Id).HasColumnName("id");

        builder.Property(nodo => nodo.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(nodo => nodo.UbicacionDentroParque)
            .HasColumnName("ubicacion_dentro_parque")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(nodo => nodo.ParqueSolarId)
            .HasColumnName("parque_solar_id")
            .IsRequired();

        // Regla de negocio 1: un nodo tiene exactamente un inversor.
        builder.HasOne(nodo => nodo.Inversor)
            .WithOne()
            .HasForeignKey<Inversor>(inversor => inversor.NodoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(nodo => nodo.Paneles)
            .WithOne()
            .HasForeignKey(panel => panel.NodoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
