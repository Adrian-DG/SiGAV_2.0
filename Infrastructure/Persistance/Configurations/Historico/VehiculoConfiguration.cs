using Domain.Entities.Historico;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Historico;

internal sealed class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> builder)
    {
        builder.ToTable("vehiculos", Schemas.Historico);
        builder.ConfigureBaseEntity();

        builder.HasIndex(v => v.Placa).IsUnique();
        builder.HasOne(v => v.Tipo)
            .WithMany()
            .HasForeignKey(v => v.TipoId);
        builder.HasOne(v => v.Color)
            .WithMany()
            .HasForeignKey(v => v.ColorId);
        builder.HasOne(v => v.Modelo)
            .WithMany()
            .HasForeignKey(v => v.ModeloId);
    }
}
