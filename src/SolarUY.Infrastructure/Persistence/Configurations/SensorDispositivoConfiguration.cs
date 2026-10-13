using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolarUY.Domain.Entities;

namespace SolarUY.Infrastructure.Persistence.Configurations;

public class SensorDispositivoConfiguration : IEntityTypeConfiguration<SensorDispositivo>
{
    public void Configure(EntityTypeBuilder<SensorDispositivo> builder)
    {
        builder.ToTable("sensores_dispositivos");

        builder.HasKey(sensor => sensor.Id);
        builder.Property(sensor => sensor.Id).HasColumnName("id");

        builder.Property(sensor => sensor.IdentificadorHardwareARM)
            .HasColumnName("identificador_hardware_arm")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sensor => sensor.VersionFirmware)
            .HasColumnName("version_firmware")
            .HasMaxLength(50);

        builder.Property(sensor => sensor.UrlBase)
            .HasColumnName("url_base")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(sensor => sensor.Activo)
            .HasColumnName("activo")
            .IsRequired();

        builder.Property(sensor => sensor.InversorId)
            .HasColumnName("inversor_id")
            .IsRequired();
    }
}
