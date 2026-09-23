using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class TramoConfiguration : IEntityTypeConfiguration<Tramo>
{
    public void Configure(EntityTypeBuilder<Tramo> builder)
    {
        builder.ToTable("tramos", Schemas.Operaciones);
        builder.ConfigureBaseEntity();

        builder.HasOne(t => t.RegionAsistencia)
            .WithMany()
            .HasForeignKey(t => t.RegionAsistenciaId);
    }
}
