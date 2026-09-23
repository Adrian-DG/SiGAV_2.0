using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Misc;

internal sealed class ModeloConfiguration : IEntityTypeConfiguration<Modelo>
{
    public void Configure(EntityTypeBuilder<Modelo> builder)
    {
        builder.ToTable("modelos", Schemas.Misc);
        builder.ConfigureBaseEntity();

        builder.HasOne(m => m.Marca)
            .WithMany()
            .HasForeignKey(m => m.MarcaId);
        builder.HasOne(m => m.TipoVehiculo)
            .WithMany()
            .HasForeignKey(m => m.TipoVehiculoId);
    }
}
