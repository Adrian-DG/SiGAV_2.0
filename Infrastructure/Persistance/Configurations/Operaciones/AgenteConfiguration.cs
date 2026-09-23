using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class AgenteConfiguration : IEntityTypeConfiguration<Agente>
{
    public void Configure(EntityTypeBuilder<Agente> builder)
    {
        builder.ToTable("agentes", Schemas.Operaciones);
        builder.ConfigurePerson();

        builder.HasOne(a => a.Rango)
            .WithMany()
            .HasForeignKey(a => a.RangoId);
    }
}
