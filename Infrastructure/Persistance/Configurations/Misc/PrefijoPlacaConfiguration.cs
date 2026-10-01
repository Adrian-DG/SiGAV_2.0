using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Misc;

internal sealed class PrefijoPlacaConfiguration : IEntityTypeConfiguration<PrefijoPlaca>
{
    public void Configure(EntityTypeBuilder<PrefijoPlaca> builder)
    {
        builder.ToTable("prefijos_placa", Schemas.Misc);
        builder.ConfigureBaseEntity();

        builder.Property(p => p.Prefijo).HasMaxLength(PrefijoPlaca.PrefijoMaxLength);
        builder.Property(p => p.Patron).HasMaxLength(PrefijoPlaca.PatronMaxLength);
        builder.Property(p => p.Ejemplo).HasMaxLength(PrefijoPlaca.EjemploMaxLength);
        builder.HasIndex(p => p.Prefijo).IsUnique();

        builder.HasMany(p => p.TiposVehiculo)
            .WithMany()
            .UsingEntity(
                "prefijo_placa_tipo_vehiculo",
                r => r.HasOne(typeof(TipoVehiculo)).WithMany().HasForeignKey("TipoVehiculoId").OnDelete(DeleteBehavior.Cascade),
                l => l.HasOne(typeof(PrefijoPlaca)).WithMany().HasForeignKey("PrefijoPlacaId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("prefijo_placa_tipo_vehiculo", Schemas.Misc);
                    j.HasKey("PrefijoPlacaId", "TipoVehiculoId");
                });
    }
}
