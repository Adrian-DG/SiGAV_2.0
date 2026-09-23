using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Misc;

internal sealed class RangoConfiguration : IEntityTypeConfiguration<Rango>
{
    public void Configure(EntityTypeBuilder<Rango> builder)
    {
        builder.ToTable("rangos", Schemas.Misc);
        builder.ConfigureBaseEntity();
    }
}
