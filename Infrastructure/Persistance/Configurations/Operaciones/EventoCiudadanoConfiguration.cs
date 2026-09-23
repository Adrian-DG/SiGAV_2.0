using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoCiudadanoConfiguration : IEntityTypeConfiguration<EventoCiudadano>
{
    public void Configure(EntityTypeBuilder<EventoCiudadano> builder)
    {
        builder.ToTable("evento_ciudadano", Schemas.Operaciones);
        builder.HasKey(ec => new { ec.EventoId, ec.CiudadanoId });

        builder.HasOne(ec => ec.Evento)
            .WithMany(e => e.Ciudadanos)
            .HasForeignKey(ec => ec.EventoId);
        builder.HasOne(ec => ec.Ciudadano)
            .WithMany()
            .HasForeignKey(ec => ec.CiudadanoId);
        builder.HasOne(ec => ec.Vehiculo)
            .WithMany()
            .HasForeignKey(ec => ec.VehiculoId);
    }
}
