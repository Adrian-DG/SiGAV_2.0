using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Misc;

internal sealed class HospitalConfiguration : IEntityTypeConfiguration<Hospital>
{
    public void Configure(EntityTypeBuilder<Hospital> builder)
    {
        builder.ToTable("hospitales", Schemas.Misc);
        builder.ConfigureBaseEntity();
        builder.Property(h => h.Nombre).HasMaxLength(Hospital.NombreMaxLength);
        builder.HasIndex(h => h.Nombre).IsUnique();

        builder.HasOne(h => h.Provincia)
            .WithMany()
            .HasForeignKey(h => h.ProvinciaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
