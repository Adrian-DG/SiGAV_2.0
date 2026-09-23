using Domain.Entities.Operaciones;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoUnidadConfiguration : IEntityTypeConfiguration<EventoUnidad>
{
    public void Configure(EntityTypeBuilder<EventoUnidad> builder)
    {
        builder.ToTable("evento_unidad", Schemas.Operaciones);
        builder.HasKey(eu => new { eu.EventoId, eu.UnidadId });

        // Parte del agregado Evento
        builder.HasOne(eu => eu.Evento)
            .WithMany(e => e.Unidades)
            .HasForeignKey(eu => eu.EventoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Referencias a otros agregados/catálogos: nunca en cascada
        builder.HasOne(eu => eu.Unidad)
            .WithMany()
            .HasForeignKey(eu => eu.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(eu => eu.Agente)
            .WithMany()
            .HasForeignKey(eu => eu.AgenteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Nivel y denominación quedan como foto del momento del evento (las estadísticas
        // no cambian si luego se reasigna la denominación o se cambia su nivel)
        builder.HasOne(eu => eu.NivelDenominacion)
            .WithMany()
            .HasForeignKey(eu => eu.NivelDenominacionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(eu => eu.Denominacion)
            .WithMany()
            .HasForeignKey(eu => eu.DenominacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(eu => new { eu.DenominacionId, eu.EventoId });

        // Garantía en BD además del dominio: una sola unidad principal por evento
        builder.HasIndex(eu => eu.EventoId)
            .IsUnique()
            .HasFilter($"[Rol] = {(int)RolUnidadEventoEnum.Principal}")
            .HasDatabaseName("UX_evento_unidad_principal");
    }
}
