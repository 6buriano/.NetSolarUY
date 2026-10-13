using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarUY.Domain.Entities;

namespace SolarUY.Infrastructure.Persistence.Configurations;

public class LecturaTelemetriaConfiguration : IEntityTypeConfiguration<LecturaTelemetria>
{
    public void Configure(EntityTypeBuilder<LecturaTelemetria> builder)
    {
        builder.ToTable("lecturas_telemetria");

        builder.HasKey(lectura => lectura.Id);
        builder.Property(lectura => lectura.Id).HasColumnName("id");

        builder.Property(lectura => lectura.PanelSolarId)
            .HasColumnName("panel_solar_id")
            .IsRequired();

        builder.Property(lectura => lectura.FechaHora)
            .HasColumnName("fecha_hora")
            .IsRequired();

        builder.Property(lectura => lectura.CorrienteContinua)
            .HasColumnName("corriente_continua")
            .IsRequired();

        builder.Property(lectura => lectura.Voltaje)
            .HasColumnName("voltaje")
            .IsRequired();

        builder.Property(lectura => lectura.PotenciaGeneradaWatts)
            .HasColumnName("potencia_generada_watts")
            .IsRequired();

        builder.Property(lectura => lectura.TemperaturaCelsius)
            .HasColumnName("temperatura_celsius")
            .IsRequired();

        builder.Property(lectura => lectura.EstadoDetectado)
            .HasColumnName("estado_detectado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Acelera la consulta de "última lectura de un panel" y los gráficos por rango de fechas.
        builder.HasIndex(lectura => new { lectura.PanelSolarId, lectura.FechaHora });
    }
}
