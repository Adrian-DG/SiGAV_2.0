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

        // Idempotencia: un reenvío con la misma clave no puede insertar otra evidencia
        builder.HasIndex(e => new { e.EventoId, e.RequestId })
            .IsUnique()
            .HasFilter("[RequestId] IS NOT NULL");

        // Persona o vehículo del mismo evento. Restrict (no cascade): el evento ya borra todo en
        // cascada y dos rutas de cascada hacia la misma fila no se permiten en todos los motores.
        builder.HasOne(e => e.Ciudadano)
            .WithMany()
            .HasForeignKey(e => e.EventoCiudadanoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Vehiculo)
            .WithMany()
            .HasForeignKey(e => e.EventoVehiculoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
