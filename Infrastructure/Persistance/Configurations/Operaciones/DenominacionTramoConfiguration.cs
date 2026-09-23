using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class DenominacionTramoConfiguration : IEntityTypeConfiguration<DenominacionTramo>
{
    public void Configure(EntityTypeBuilder<DenominacionTramo> builder)
    {
        builder.ToTable("denominacion_tramos", Schemas.Operaciones);
        builder.HasKey(t => new { t.DenominacionId, t.TramoId });

        builder.HasOne(t => t.Tramo)
            .WithMany()
            .HasForeignKey(t => t.TramoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
