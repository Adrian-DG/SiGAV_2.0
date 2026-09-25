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

        // Foto de la persona y del vehículo, en columnas de la misma tabla
        builder.OwnsOne(ec => ec.Persona, persona =>
        {
            persona.Property(p => p.Identificacion).HasColumnName("Identificacion").HasMaxLength(DatosPersona.IdentificacionMaxLength);
            persona.Property(p => p.Nombre).HasColumnName("Nombre").HasMaxLength(DatosPersona.NombreMaxLength);
            persona.Property(p => p.Apellido).HasColumnName("Apellido").HasMaxLength(DatosPersona.NombreMaxLength);
            persona.Property(p => p.Sexo).HasColumnName("Sexo");
            persona.Property(p => p.Telefono).HasColumnName("Telefono").HasMaxLength(DatosPersona.TelefonoMaxLength);
            persona.Property(p => p.NacionalidadId).HasColumnName("NacionalidadId");
            persona.HasIndex(p => p.Identificacion);
            persona.HasOne<Domain.Entities.Misc.Nacionalidad>()
                .WithMany()
                .HasForeignKey(p => p.NacionalidadId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(ec => ec.Persona).IsRequired();

        builder.OwnsOne(ec => ec.Vehiculo, vehiculo =>
        {
            vehiculo.Property(v => v.Placa).HasColumnName("Placa").HasMaxLength(DatosVehiculo.PlacaMaxLength);
            vehiculo.Property(v => v.TipoVehiculoId).HasColumnName("TipoVehiculoId");
            vehiculo.Property(v => v.MarcaId).HasColumnName("MarcaId");
            vehiculo.Property(v => v.ModeloId).HasColumnName("ModeloId");
            vehiculo.Property(v => v.ColorId).HasColumnName("ColorId");
            vehiculo.Property(v => v.MarcaTexto).HasColumnName("MarcaTexto").HasMaxLength(DatosVehiculo.TextoMaxLength);
            vehiculo.Property(v => v.ModeloTexto).HasColumnName("ModeloTexto").HasMaxLength(DatosVehiculo.TextoMaxLength);
            vehiculo.Property(v => v.ColorTexto).HasColumnName("ColorTexto").HasMaxLength(DatosVehiculo.TextoMaxLength);
            vehiculo.HasIndex(v => v.Placa);
            vehiculo.HasOne<Domain.Entities.Misc.TipoVehiculo>().WithMany().HasForeignKey(v => v.TipoVehiculoId).OnDelete(DeleteBehavior.Restrict);
            vehiculo.HasOne<Domain.Entities.Misc.Marca>().WithMany().HasForeignKey(v => v.MarcaId).OnDelete(DeleteBehavior.Restrict);
            vehiculo.HasOne<Domain.Entities.Misc.Modelo>().WithMany().HasForeignKey(v => v.ModeloId).OnDelete(DeleteBehavior.Restrict);
            vehiculo.HasOne<Domain.Entities.Misc.Color>().WithMany().HasForeignKey(v => v.ColorId).OnDelete(DeleteBehavior.Restrict);
        });

        // Vínculos opcionales con los maestros históricos
        builder.HasOne(ec => ec.Ciudadano)
            .WithMany()
            .HasForeignKey(ec => ec.CiudadanoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(ec => ec.Vehiculo)
            .WithMany()
            .HasForeignKey(ec => ec.VehiculoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
