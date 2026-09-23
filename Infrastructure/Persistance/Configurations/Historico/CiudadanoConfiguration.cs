using Domain.Entities.Historico;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Historico;

internal sealed class CiudadanoConfiguration : IEntityTypeConfiguration<Ciudadano>
{
    public void Configure(EntityTypeBuilder<Ciudadano> builder)
    {
        builder.ToTable("ciudadanos", Schemas.Historico);
        builder.ConfigurePerson();

        builder.HasIndex(c => c.Identificacion).IsUnique();
        builder.HasOne(c => c.Nacionalidad)
            .WithMany()
            .HasForeignKey(c => c.NacionalidadId);
    }
}
