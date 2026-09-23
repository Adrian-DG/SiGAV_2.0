using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class NivelDenominacionConfiguration : IEntityTypeConfiguration<NivelDenominacion>
{
    public void Configure(EntityTypeBuilder<NivelDenominacion> builder)
    {
        builder.ToTable("niveles_denominacion", Schemas.Operaciones);
        builder.ConfigureBaseEntity();
    }
}
