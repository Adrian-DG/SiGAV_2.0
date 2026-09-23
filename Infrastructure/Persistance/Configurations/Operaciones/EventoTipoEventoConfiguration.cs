using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoTipoEventoConfiguration : IEntityTypeConfiguration<EventoTipoEvento>
{
    public void Configure(EntityTypeBuilder<EventoTipoEvento> builder)
    {
        builder.ToTable("evento_tipo_evento", Schemas.Operaciones);
        builder.HasKey(et => new { et.EventoId, et.TipoEventoId });

        builder.HasOne(et => et.Evento)
            .WithMany(e => e.Tipos)
            .HasForeignKey(et => et.EventoId);
        builder.HasOne(et => et.TipoEvento)
            .WithMany()
            .HasForeignKey(et => et.TipoEventoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
