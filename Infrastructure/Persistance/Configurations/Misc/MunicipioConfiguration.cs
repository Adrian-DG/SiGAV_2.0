using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Misc;

internal sealed class MunicipioConfiguration : IEntityTypeConfiguration<Municipio>
{
    public void Configure(EntityTypeBuilder<Municipio> builder)
    {
        builder.ToTable("municipios", Schemas.Misc);
        builder.ConfigureBaseEntity();

        builder.HasOne(m => m.Provincia)
            .WithMany()
            .HasForeignKey(m => m.ProvinciaId);
    }
}
