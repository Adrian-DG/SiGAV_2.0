using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class FlotaConfiguration : IEntityTypeConfiguration<Flota>
{
    public void Configure(EntityTypeBuilder<Flota> builder)
    {
        builder.ToTable("flotas", Schemas.Operaciones);
        builder.ConfigureBaseEntity();

        builder.HasIndex(f => new { f.Numero, f.Codigo }).IsUnique();
        builder.HasOne(f => f.Denominacion)
            .WithMany()
            .HasForeignKey(f => f.DenominacionId);
    }
}
