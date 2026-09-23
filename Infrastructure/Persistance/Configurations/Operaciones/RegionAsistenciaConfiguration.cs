using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class RegionAsistenciaConfiguration : IEntityTypeConfiguration<RegionAsistencia>
{
    public void Configure(EntityTypeBuilder<RegionAsistencia> builder)
    {
        builder.ToTable("regiones_asistencia", Schemas.Operaciones);
        builder.ConfigureBaseEntity();
    }
}
