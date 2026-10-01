using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class TipoCierreConfiguration : IEntityTypeConfiguration<TipoCierre>
{
    public void Configure(EntityTypeBuilder<TipoCierre> builder)
    {
        builder.ToTable("tipo_cierres", Schemas.Operaciones);
        builder.ConfigureBaseEntity();
    }
}
