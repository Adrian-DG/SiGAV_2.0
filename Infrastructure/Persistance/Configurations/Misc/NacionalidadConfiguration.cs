using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Misc;

internal sealed class NacionalidadConfiguration : IEntityTypeConfiguration<Nacionalidad>
{
    public void Configure(EntityTypeBuilder<Nacionalidad> builder)
    {
        builder.ToTable("nacionalidades", Schemas.Misc);
        builder.ConfigureBaseEntity();
    }
}
