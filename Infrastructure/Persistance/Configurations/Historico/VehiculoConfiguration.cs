using Domain.Entities.Historico;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Historico;

internal sealed class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> builder)
    {
        builder.ToTable("vehiculos", Schemas.Historico);
        builder.ConfigureBaseEntity();

        // Mismos límites que la foto del evento: todo lo que acepta un evento cabe en el maestro
        builder.Property(v => v.Placa).HasMaxLength(DatosVehiculo.PlacaMaxLength);
        builder.Property(v => v.MarcaTexto).HasMaxLength(DatosVehiculo.TextoMaxLength);
        builder.Property(v => v.ModeloTexto).HasMaxLength(DatosVehiculo.TextoMaxLength);
        builder.Property(v => v.ColorTexto).HasMaxLength(DatosVehiculo.TextoMaxLength);

        builder.HasIndex(v => v.Placa).IsUnique();
        builder.HasOne(v => v.TipoVehiculo)
            .WithMany()
            .HasForeignKey(v => v.TipoVehiculoId)
            .IsRequired(false);
        builder.HasOne(v => v.Marca)
            .WithMany()
            .HasForeignKey(v => v.MarcaId)
            .IsRequired(false);
        builder.HasOne(v => v.Modelo)
            .WithMany()
            .HasForeignKey(v => v.ModeloId)
            .IsRequired(false);
        builder.HasOne(v => v.Color)
            .WithMany()
            .HasForeignKey(v => v.ColorId)
            .IsRequired(false);
    }
}
