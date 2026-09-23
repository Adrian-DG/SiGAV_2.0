using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Misc;

internal sealed class ColorConfiguration : IEntityTypeConfiguration<Domain.Entities.Misc.Color>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Misc.Color> builder)
    {
        builder.ToTable("colores", Schemas.Misc);
        builder.ConfigureBaseEntity();
    }
}
