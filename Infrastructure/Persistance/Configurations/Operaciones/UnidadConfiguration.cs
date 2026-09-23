using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class UnidadConfiguration : IEntityTypeConfiguration<Unidad>
{
    public void Configure(EntityTypeBuilder<Unidad> builder)
    {
        builder.ToTable("unidades", Schemas.Operaciones);
        builder.ConfigureBaseEntity();

        builder.Property(u => u.Ficha).HasMaxLength(Unidad.FichaMaxLength);
        builder.Property(u => u.Placa).HasMaxLength(Unidad.PlacaMaxLength);
        builder.HasIndex(u => u.Ficha).IsUnique();
        builder.HasIndex(u => u.DenominacionId);

        builder.HasOne(u => u.Denominacion)
            .WithMany()
            .HasForeignKey(u => u.DenominacionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
