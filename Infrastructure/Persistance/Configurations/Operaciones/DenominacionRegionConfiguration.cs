using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class DenominacionRegionConfiguration : IEntityTypeConfiguration<DenominacionRegion>
{
    public void Configure(EntityTypeBuilder<DenominacionRegion> builder)
    {
        builder.ToTable("denominacion_regiones", Schemas.Operaciones, t => t.HasCheckConstraint(
            "CK_denominacion_regiones_macro_o_asistencia",
            "([RegionMacro] IS NOT NULL AND [RegionAsistenciaId] IS NULL) OR ([RegionMacro] IS NULL AND [RegionAsistenciaId] IS NOT NULL)"));
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.DenominacionId, r.RegionMacro })
            .IsUnique()
            .HasFilter("[RegionMacro] IS NOT NULL");
        builder.HasIndex(r => new { r.DenominacionId, r.RegionAsistenciaId })
            .IsUnique()
            .HasFilter("[RegionAsistenciaId] IS NOT NULL");

        builder.HasOne(r => r.RegionAsistencia)
            .WithMany()
            .HasForeignKey(r => r.RegionAsistenciaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
