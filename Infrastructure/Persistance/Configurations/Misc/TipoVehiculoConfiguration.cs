using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Misc;

internal sealed class TipoVehiculoConfiguration : IEntityTypeConfiguration<TipoVehiculo>
{
    public void Configure(EntityTypeBuilder<TipoVehiculo> builder)
    {
        builder.ToTable("tipo_vehiculos", Schemas.Misc);
        builder.ConfigureBaseEntity();
    }
}
