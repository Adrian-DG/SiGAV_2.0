using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoEvidenciaConfiguration : IEntityTypeConfiguration<EventoEvidencia>
{
    public void Configure(EntityTypeBuilder<EventoEvidencia> builder)
    {
        builder.ToTable("evento_evidencias", Schemas.Operaciones);
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Ubicacion).HasMaxLength(EventoEvidencia.UbicacionMaxLength);
        builder.Property(e => e.ContentType).HasMaxLength(EventoEvidencia.ContentTypeMaxLength);
        builder.HasIndex(e => new { e.EventoId, e.Tipo });
    }
}
