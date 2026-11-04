using Domain.Entities.Calidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Calidad;

internal sealed class EvaluacionCalidadConfiguration : IEntityTypeConfiguration<EvaluacionCalidad>
{
    public void Configure(EntityTypeBuilder<EvaluacionCalidad> builder)
    {
        builder.ToTable("evaluaciones", Schemas.Calidad);
        builder.ConfigureBaseEntity();

        // Una evaluación por evento: si dos evaluadores la inician a la vez, el segundo recibe 409
        builder.HasIndex(e => e.EventoId).IsUnique();
        builder.HasIndex(e => new { e.Estado, e.CerradaUtc });

        builder.Property(e => e.TelefonoContacto).HasMaxLength(EvaluacionCalidad.TelefonoMaxLength);
        builder.Property(e => e.NombreContacto).HasMaxLength(EvaluacionCalidad.NombreContactoMaxLength);
        builder.Property(e => e.Observacion).HasMaxLength(EvaluacionCalidad.ObservacionMaxLength);

        builder.HasOne(e => e.Evento)
            .WithMany()
            .HasForeignKey(e => e.EventoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Encuesta)
            .WithMany()
            .HasForeignKey(e => e.EncuestaId)
            .OnDelete(DeleteBehavior.Restrict);
        // Un evento completado ya no cambia sus personas, así que la referencia es estable
        builder.HasOne(e => e.EventoCiudadano)
            .WithMany()
            .HasForeignKey(e => e.EventoCiudadanoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Intentos)
            .WithOne()
            .HasForeignKey(i => i.EvaluacionCalidadId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Respuestas)
            .WithOne()
            .HasForeignKey(r => r.EvaluacionCalidadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.Contesto);
    }
}

internal sealed class IntentoContactoConfiguration : IEntityTypeConfiguration<IntentoContacto>
{
    public void Configure(EntityTypeBuilder<IntentoContacto> builder)
    {
        builder.ToTable("intentos_contacto", Schemas.Calidad);
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Observacion).HasMaxLength(IntentoContacto.ObservacionMaxLength);
        builder.Property(i => i.ReferenciaExterna).HasMaxLength(IntentoContacto.ReferenciaExternaMaxLength);
        builder.HasIndex(i => new { i.EvaluacionCalidadId, i.FechaUtc });
        // Idempotencia del futuro webhook de mensajería
        builder.HasIndex(i => i.ReferenciaExterna)
            .IsUnique()
            .HasFilter("[ReferenciaExterna] IS NOT NULL");
    }
}

internal sealed class RespuestaEvaluacionConfiguration : IEntityTypeConfiguration<RespuestaEvaluacion>
{
    public void Configure(EntityTypeBuilder<RespuestaEvaluacion> builder)
    {
        builder.ToTable("respuestas_evaluacion", Schemas.Calidad);
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Texto).HasMaxLength(RespuestaEvaluacion.TextoMaxLength);
        builder.Property(r => r.Comentario).HasMaxLength(RespuestaEvaluacion.ComentarioMaxLength);
        builder.HasIndex(r => new { r.EvaluacionCalidadId, r.PreguntaEncuestaId }).IsUnique();

        // La pregunta pertenece a una versión publicada, que no se borra
        builder.HasOne(r => r.Pregunta)
            .WithMany()
            .HasForeignKey(r => r.PreguntaEncuestaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
