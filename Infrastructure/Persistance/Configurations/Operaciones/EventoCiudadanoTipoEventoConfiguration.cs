using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoCiudadanoTipoEventoConfiguration : IEntityTypeConfiguration<EventoCiudadanoTipoEvento>
{
    public void Configure(EntityTypeBuilder<EventoCiudadanoTipoEvento> builder)
    {
        builder.ToTable("evento_ciudadano_tipo_evento", Schemas.Operaciones);
        builder.HasKey(ct => new { ct.EventoCiudadanoId, ct.TipoEventoId });

        // Parte del agregado Evento (a través de la persona)
        builder.HasOne(ct => ct.EventoCiudadano)
            .WithMany(c => c.Tipos)
            .HasForeignKey(ct => ct.EventoCiudadanoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ct => ct.TipoEvento)
            .WithMany()
            .HasForeignKey(ct => ct.TipoEventoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
