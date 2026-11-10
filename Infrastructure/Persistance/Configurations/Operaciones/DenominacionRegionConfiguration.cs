using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class DenominacionRegionConfiguration : IEntityTypeConfiguration<DenominacionRegion>
{
    public void Configure(EntityTypeBuilder<DenominacionRegion> builder)
    {
        builder.ToTable("denominacion_regiones", Schemas.Operaciones, t => t.HasCheckConstraint(
            "CK_denominacion_regiones_macro_o_zona",
            "([RegionMacro] IS NOT NULL AND [ZonaId] IS NULL) OR ([RegionMacro] IS NULL AND [ZonaId] IS NOT NULL)"));
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.DenominacionId, r.RegionMacro })
            .IsUnique()
            .HasFilter("[RegionMacro] IS NOT NULL");
        builder.HasIndex(r => new { r.DenominacionId, r.ZonaId })
            .IsUnique()
            .HasFilter("[ZonaId] IS NOT NULL");

        builder.HasOne(r => r.Zona)
            .WithMany()
            .HasForeignKey(r => r.ZonaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
