using Domain.Entities.Operaciones;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoCiudadanoConfiguration : IEntityTypeConfiguration<EventoCiudadanoInfo>
{
    public void Configure(EntityTypeBuilder<EventoCiudadanoInfo> builder)
    {
        builder.ToTable("evento_ciudadano", Schemas.Operaciones);
        builder.HasKey(ec => ec.Id);

        // Parte del agregado Evento
        builder.HasOne(ec => ec.Evento)
            .WithMany(e => e.Ciudadanos)
            .HasForeignKey(ec => ec.EventoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foto de la persona, en columnas de la misma tabla
        builder.OwnsOne(ec => ec.Persona, persona =>
        {
            persona.Property(p => p.Identificacion).HasColumnName("Identificacion").HasMaxLength(DatosPersona.IdentificacionMaxLength);
            persona.Property(p => p.Nombre).HasColumnName("Nombre").HasMaxLength(DatosPersona.NombreMaxLength);
            persona.Property(p => p.Apellido).HasColumnName("Apellido").HasMaxLength(DatosPersona.NombreMaxLength);
            persona.Property(p => p.Sexo).HasColumnName("Sexo");
            persona.Property(p => p.Edad).HasColumnName("Edad");
            persona.Property(p => p.Telefono).HasColumnName("Telefono").HasMaxLength(DatosPersona.TelefonoMaxLength);
            persona.Property(p => p.NacionalidadId).HasColumnName("NacionalidadId");
            persona.HasIndex(p => p.Identificacion);
            persona.HasOne<Domain.Entities.Misc.Nacionalidad>()
                .WithMany()
                .HasForeignKey(p => p.NacionalidadId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(ec => ec.Persona).IsRequired();

        // Vehículo del mismo evento. Restrict (no cascade): el evento ya borra ambos en cascada y
        // dos rutas de cascada hacia la misma fila no se permiten en todos los motores.
        builder.HasOne(ec => ec.Vehiculo)
            .WithMany()
            .HasForeignKey(ec => ec.EventoVehiculoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Atención prehospitalaria (0..1), en su propia tabla: la mayoría de las personas no la tiene
        builder.OwnsOne(ec => ec.Atencion, atencion =>
        {
            atencion.ToTable("evento_atencion_prehospitalaria", Schemas.Operaciones);
            atencion.WithOwner().HasForeignKey("EventoCiudadanoId");
            atencion.Property<int>("EventoCiudadanoId");
            atencion.HasKey("EventoCiudadanoId");

            atencion.Property(a => a.NombreEventoEspecial).HasMaxLength(AtencionPrehospitalaria.TextoCortoMaxLength);
            atencion.Property(a => a.RecibidoPor).HasMaxLength(AtencionPrehospitalaria.TextoCortoMaxLength);
            atencion.Property(a => a.Antecedentes).HasMaxLength(AtencionPrehospitalaria.TextoLargoMaxLength);
            atencion.Property(a => a.HallazgosPositivos).HasMaxLength(AtencionPrehospitalaria.TextoLargoMaxLength);
            atencion.Property(a => a.DiagnosticoPresuntivo).HasMaxLength(AtencionPrehospitalaria.TextoLargoMaxLength);
            atencion.Property(a => a.ProcedimientosRealizados).HasMaxLength(AtencionPrehospitalaria.TextoLargoMaxLength);
            atencion.Property(a => a.InsumosUtilizados).HasMaxLength(AtencionPrehospitalaria.TextoLargoMaxLength);

            atencion.HasOne(a => a.Hospital)
                .WithMany()
                .HasForeignKey(a => a.HospitalId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            atencion.HasOne(a => a.Medico)
                .WithMany()
                .HasForeignKey(a => a.MedicoAgenteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            atencion.HasOne(a => a.Regulador)
                .WithMany()
                .HasForeignKey(a => a.ReguladorAgenteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            atencion.HasIndex(a => a.HospitalId);

            atencion.OwnsMany(a => a.SignosVitales, toma =>
            {
                toma.ToTable("evento_signos_vitales", Schemas.Operaciones);
                toma.WithOwner().HasForeignKey("EventoCiudadanoId");
                toma.Property<int>("Id").ValueGeneratedOnAdd();
                toma.HasKey("Id");
                toma.Property(t => t.Temperatura).HasPrecision(4, 1);
                toma.Ignore(t => t.GlasgowTotal);
            });
            atencion.Navigation(a => a.SignosVitales).HasField("_signosVitales");
        });

        // Vínculo opcional con el maestro histórico
        builder.HasOne(ec => ec.Ciudadano)
            .WithMany()
            .HasForeignKey(ec => ec.CiudadanoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
