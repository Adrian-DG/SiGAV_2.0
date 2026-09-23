using Domain.Abstraction;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// Mapeo común de las clases base del dominio (antes expresado con DataAnnotations).
/// </summary>
internal static class MetadataConfigurationExtensions
{
    public static EntityTypeBuilder<T> ConfigureBaseEntity<T>(this EntityTypeBuilder<T> builder) where T : BaseEntityMetadata
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        return builder;
    }

    public static EntityTypeBuilder<T> ConfigurePerson<T>(this EntityTypeBuilder<T> builder) where T : PersonMetadata
    {
        builder.ConfigureBaseEntity();
        builder.Property(p => p.Identificacion).HasMaxLength(PersonMetadata.IdentificacionMaxLength);
        builder.Property(p => p.Nombre).HasMaxLength(PersonMetadata.NombreMaxLength);
        builder.Property(p => p.Apellido).HasMaxLength(PersonMetadata.ApellidoMaxLength);
        return builder;
    }
}
