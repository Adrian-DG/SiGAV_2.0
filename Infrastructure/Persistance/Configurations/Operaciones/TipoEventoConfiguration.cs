using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class TipoEventoConfiguration : IEntityTypeConfiguration<TipoEvento>
{
    public void Configure(EntityTypeBuilder<TipoEvento> builder)
    {
        builder.ToTable("tipo_eventos", Schemas.Operaciones);
        builder.ConfigureBaseEntity();
    }
}
