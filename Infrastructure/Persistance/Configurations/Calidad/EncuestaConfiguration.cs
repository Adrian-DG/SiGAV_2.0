using Domain.Entities.Calidad;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Calidad;

internal sealed class EncuestaConfiguration : IEntityTypeConfiguration<Encuesta>
{
    public void Configure(EntityTypeBuilder<Encuesta> builder)
    {
        builder.ToTable("encuestas", Schemas.Calidad);
        builder.ConfigureBaseEntity();

        builder.Property(e => e.Nombre).HasMaxLength(Encuesta.NombreMaxLength);
        builder.HasIndex(e => e.Version).IsUnique();

        // Una sola publicada (vigente) y un solo borrador: dos publicaciones simultáneas chocan aquí
        builder.HasIndex(e => e.Estado)
            .IsUnique()
            .HasFilter($"[Estado] IN ({(int)EstadoEncuestaEnum.Borrador}, {(int)EstadoEncuestaEnum.Publicada})");

        // Preguntas y opciones: parte del agregado (reemplazar las de un borrador borra las anteriores)
        builder.HasMany(e => e.Preguntas)
            .WithOne()
            .HasForeignKey(p => p.EncuestaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Preguntas).HasField("_preguntas");
    }
}

internal sealed class PreguntaEncuestaConfiguration : IEntityTypeConfiguration<PreguntaEncuesta>
{
    public void Configure(EntityTypeBuilder<PreguntaEncuesta> builder)
    {
        builder.ToTable("preguntas_encuesta", Schemas.Calidad);
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Texto).HasMaxLength(PreguntaEncuesta.TextoMaxLength);
        builder.HasIndex(p => new { p.EncuestaId, p.Orden });

        builder.HasMany(p => p.Opciones)
            .WithOne()
            .HasForeignKey(o => o.PreguntaEncuestaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Opciones).HasField("_opciones");
    }
}

internal sealed class OpcionPreguntaConfiguration : IEntityTypeConfiguration<OpcionPregunta>
{
    public void Configure(EntityTypeBuilder<OpcionPregunta> builder)
    {
        builder.ToTable("opciones_pregunta", Schemas.Calidad);
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Etiqueta).HasMaxLength(PreguntaEncuesta.EtiquetaMaxLength);
        builder.HasIndex(o => o.PreguntaEncuestaId);
    }
}
