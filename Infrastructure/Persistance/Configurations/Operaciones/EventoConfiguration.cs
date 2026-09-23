using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> builder)
    {
        builder.ToTable("eventos", Schemas.Operaciones);
        builder.ConfigureBaseEntity();

        // Idempotencia: un reenvío con la misma clave no puede insertar un segundo evento.
        // Filtrado para que los eventos sin clave (históricos, web) no choquen entre sí.
        builder.HasIndex(e => e.RequestId)
            .IsUnique()
            .HasFilter("[RequestId] IS NOT NULL");

        // Complex type (no owned): es un value object sin relaciones y puede compartirse entre
        // eventos; un owned type se rastrea por referencia y fallaría con instancias compartidas.
        builder.ComplexProperty(e => e.Ubicacion, ubicacion =>
        {
            ubicacion.Property(c => c.Latitud).HasColumnName("Latitud").HasPrecision(9, 6);
            ubicacion.Property(c => c.Longitud).HasColumnName("Longitud").HasPrecision(9, 6);
        });

        builder.Property(e => e.Direccion).HasMaxLength(Evento.DireccionMaxLength);
        builder.Property(e => e.Comentario).HasMaxLength(Evento.ComentarioMaxLength);

        // Estadísticas y bandejas filtran por fecha del reporte (no por la de guardado)
        builder.HasIndex(e => new { e.IsActive, e.FechaHoraReporteUtc });
        builder.HasIndex(e => e.Estado);

        builder.HasOne(e => e.Municipio)
            .WithMany()
            .HasForeignKey(e => e.MunicipioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Tramo)
            .WithMany()
            .HasForeignKey(e => e.TramoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Evidencias: parte del agregado
        builder.HasMany(e => e.Evidencias)
            .WithOne()
            .HasForeignKey(ev => ev.EventoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.UnidadPrincipal);
    }
}
