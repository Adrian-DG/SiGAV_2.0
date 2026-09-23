using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoUnidadConfiguration : IEntityTypeConfiguration<EventoUnidad>
{
    public void Configure(EntityTypeBuilder<EventoUnidad> builder)
    {
        builder.ToTable("evento_unidad", Schemas.Operaciones);
        builder.HasKey(eu => new { eu.EventoId, eu.UnidadId });

        builder.HasOne(eu => eu.Evento)
            .WithMany(e => e.Unidades)
            .HasForeignKey(eu => eu.EventoId);
        builder.HasOne(eu => eu.Unidad)
            .WithMany()
            .HasForeignKey(eu => eu.UnidadId);
        builder.HasOne(eu => eu.Agente)
            .WithMany()
            .HasForeignKey(eu => eu.AgenteId);

        // Nivel y denominación quedan como foto del momento del evento (las estadísticas
        // no cambian si luego se reasigna la denominación o se cambia su nivel)
        builder.HasOne(eu => eu.NivelDenominacion)
            .WithMany()
            .HasForeignKey(eu => eu.NivelDenominacionId);
        builder.HasOne(eu => eu.Denominacion)
            .WithMany()
            .HasForeignKey(eu => eu.DenominacionId);

        builder.HasIndex(eu => new { eu.DenominacionId, eu.EventoId });
    }
}
