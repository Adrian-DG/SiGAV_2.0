using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoTipoEventoConfiguration : IEntityTypeConfiguration<EventoTipoEvento>
{
    public void Configure(EntityTypeBuilder<EventoTipoEvento> builder)
    {
        // Lo recibe un vehículo, una persona o el evento en general (ambos null), nunca los dos
        builder.ToTable("evento_tipo_evento", Schemas.Operaciones, t => t.HasCheckConstraint(
            "CK_evento_tipo_evento_un_involucrado",
            "\"EventoVehiculoId\" IS NULL OR \"EventoCiudadanoId\" IS NULL"));
        builder.HasKey(et => et.Id);
        builder.Ignore(et => et.EsGeneral);

        // Parte del agregado Evento
        builder.HasOne(et => et.Evento)
            .WithMany(e => e.Tipos)
            .HasForeignKey(et => et.EventoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(et => et.TipoEvento)
            .WithMany()
            .HasForeignKey(et => et.TipoEventoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Vehículo o persona del mismo evento. Restrict (no cascade): el evento ya borra todo en
        // cascada y dos rutas de cascada hacia la misma fila no se permiten en todos los motores.
        builder.HasOne(et => et.Vehiculo)
            .WithMany(v => v.Tipos)
            .HasForeignKey(et => et.EventoVehiculoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(et => et.Ciudadano)
            .WithMany(c => c.Tipos)
            .HasForeignKey(et => et.EventoCiudadanoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Una fila por tipo y por quién lo recibió
        builder.HasIndex(et => new { et.EventoId, et.TipoEventoId })
            .IsUnique()
            .HasFilter("\"EventoVehiculoId\" IS NULL AND \"EventoCiudadanoId\" IS NULL");
        builder.HasIndex(et => new { et.EventoVehiculoId, et.TipoEventoId })
            .IsUnique()
            .HasFilter("\"EventoVehiculoId\" IS NOT NULL");
        builder.HasIndex(et => new { et.EventoCiudadanoId, et.TipoEventoId })
            .IsUnique()
            .HasFilter("\"EventoCiudadanoId\" IS NOT NULL");
    }
}
