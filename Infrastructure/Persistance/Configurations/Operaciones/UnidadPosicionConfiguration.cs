using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class UnidadPosicionConfiguration : IEntityTypeConfiguration<UnidadPosicion>
{
    public void Configure(EntityTypeBuilder<UnidadPosicion> builder)
    {
        builder.ToTable("unidad_posicion", Schemas.Operaciones);

        // Una fila por unidad: cada envío la actualiza
        builder.HasKey(p => p.UnidadId);
        builder.Property(p => p.UnidadId).ValueGeneratedNever();

        builder.ComplexProperty(p => p.Ubicacion, ubicacion =>
        {
            ubicacion.Property(c => c.Latitud).HasColumnName("Latitud").HasPrecision(9, 6);
            ubicacion.Property(c => c.Longitud).HasColumnName("Longitud").HasPrecision(9, 6);
        });

        builder.HasOne(p => p.Unidad)
            .WithMany()
            .HasForeignKey(p => p.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Agente)
            .WithMany()
            .HasForeignKey(p => p.AgenteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.AgenteId);
        builder.HasIndex(p => p.FechaHoraRecibidaUtc);
    }
}
