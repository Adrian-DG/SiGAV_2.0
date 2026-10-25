using Domain.Entities.Historico;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Historico;

internal sealed class CiudadanoConfiguration : IEntityTypeConfiguration<Ciudadano>
{
    public void Configure(EntityTypeBuilder<Ciudadano> builder)
    {
        builder.ToTable("ciudadanos", Schemas.Historico);
        builder.ConfigureBaseEntity();

        // Mismos límites que la foto del evento: todo lo que acepta un evento cabe en el maestro
        builder.Property(c => c.Identificacion).HasMaxLength(DatosPersona.IdentificacionMaxLength);
        builder.Property(c => c.Nombre).HasMaxLength(DatosPersona.NombreMaxLength);
        builder.Property(c => c.Apellido).HasMaxLength(DatosPersona.NombreMaxLength);
        builder.Property(c => c.Telefono).HasMaxLength(DatosPersona.TelefonoMaxLength);

        builder.HasIndex(c => c.Identificacion).IsUnique();
        builder.HasOne(c => c.Nacionalidad)
            .WithMany()
            .HasForeignKey(c => c.NacionalidadId)
            .IsRequired(false);
    }
}
