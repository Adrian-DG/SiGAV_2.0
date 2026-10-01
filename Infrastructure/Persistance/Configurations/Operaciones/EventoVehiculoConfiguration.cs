using Domain.Entities.Operaciones;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoVehiculoConfiguration : IEntityTypeConfiguration<EventoVehiculoInfo>
{
    public void Configure(EntityTypeBuilder<EventoVehiculoInfo> builder)
    {
        builder.ToTable("evento_vehiculo", Schemas.Operaciones);
        builder.HasKey(ev => ev.Id);

        // Parte del agregado Evento
        builder.HasOne(ev => ev.Evento)
            .WithMany(e => e.Vehiculos)
            .HasForeignKey(ev => ev.EventoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foto del vehículo, en columnas de la misma tabla
        builder.OwnsOne(ev => ev.Datos, vehiculo =>
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
        builder.Navigation(ev => ev.Datos).IsRequired();

        // Vínculo opcional con el maestro histórico
        builder.HasOne(ev => ev.VehiculoHistorico)
            .WithMany()
            .HasForeignKey(ev => ev.VehiculoHistoricoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
