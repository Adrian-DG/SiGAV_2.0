using Domain.Entities.Operaciones;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class HistorialDenominacionUnidadConfiguration : IEntityTypeConfiguration<HistorialDenominacionUnidad>
{
    public void Configure(EntityTypeBuilder<HistorialDenominacionUnidad> builder)
    {
        builder.ToTable("historial_denominacion_unidad", Schemas.Operaciones);
        builder.HasKey(h => h.Id);

        // Registro de auditoría: nada en cascada, las referencias no pueden borrarse mientras exista el historial
        builder.HasOne<Unidad>()
            .WithMany(u => u.HistorialDenominaciones)
            .HasForeignKey(h => h.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.UnidadRelacionada)
            .WithMany()
            .HasForeignKey(h => h.UnidadRelacionadaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.DenominacionAnterior)
            .WithMany()
            .HasForeignKey(h => h.DenominacionAnteriorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.DenominacionNueva)
            .WithMany()
            .HasForeignKey(h => h.DenominacionNuevaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(h => h.Observacion).HasMaxLength(AutorCambio.ObservacionMaxLength);
        builder.HasIndex(h => new { h.UnidadId, h.FechaUtc });
        builder.HasIndex(h => h.DenominacionAnteriorId);
        builder.HasIndex(h => h.DenominacionNuevaId);
    }
}
