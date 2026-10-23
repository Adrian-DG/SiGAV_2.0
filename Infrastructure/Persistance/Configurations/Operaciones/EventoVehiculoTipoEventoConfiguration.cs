using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoVehiculoTipoEventoConfiguration : IEntityTypeConfiguration<EventoVehiculoTipoEvento>
{
    public void Configure(EntityTypeBuilder<EventoVehiculoTipoEvento> builder)
    {
        builder.ToTable("evento_vehiculo_tipo_evento", Schemas.Operaciones);
        builder.HasKey(vt => new { vt.EventoVehiculoId, vt.TipoEventoId });

        // Parte del agregado Evento (a través del vehículo)
        builder.HasOne(vt => vt.EventoVehiculo)
            .WithMany(v => v.Tipos)
            .HasForeignKey(vt => vt.EventoVehiculoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(vt => vt.TipoEvento)
            .WithMany()
            .HasForeignKey(vt => vt.TipoEventoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
