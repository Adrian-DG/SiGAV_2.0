using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class DenominacionConfiguration : IEntityTypeConfiguration<Denominacion>
{
    public void Configure(EntityTypeBuilder<Denominacion> builder)
    {
        builder.ToTable("denominaciones", Schemas.Operaciones);
        builder.ConfigureBaseEntity();

        builder.Property(d => d.Nombre).HasMaxLength(Denominacion.NombreMaxLength);
        builder.HasIndex(d => d.Nombre).IsUnique();

        builder.HasOne(d => d.Tramo)
            .WithMany()
            .HasForeignKey(d => d.TramoId);
        builder.HasOne(d => d.Nivel)
            .WithMany()
            .HasForeignKey(d => d.NivelDenominacionId);

        // Asignaciones: forman parte del agregado; al quitarlas de la colección se eliminan
        builder.HasMany(d => d.Regiones)
            .WithOne()
            .HasForeignKey(r => r.DenominacionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Tramos)
            .WithOne()
            .HasForeignKey(t => t.DenominacionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
