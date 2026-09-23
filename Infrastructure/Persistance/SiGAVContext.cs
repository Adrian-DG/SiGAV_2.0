using System.Drawing;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Entities.Misc;
using Domain.Entities.Historico;
using Domain.Entities.Operaciones;
using Domain.Repositories;
using Domain.ValueObjects;

namespace Infrastructure.Persistance;

public class SiGAVContext(DbContextOptions<SiGAVContext> options) : IdentityDbContext<AppUser, AppPermission, int>(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        #region Relations

        builder.Entity<AppUser>(entity =>
        {
            entity.ToTable("usuarios", "accesos");
            entity.HasIndex(u => new { u.Identificacion, u.UserName }).IsUnique();
        });
        
        builder.Entity<AppPermission>(entity =>
        {
            entity.ToTable("permisos", "accesos");
            entity.HasIndex(p => p.Name).IsUnique();
        });
        
        builder.Entity<Ciudadano>(entity =>
        {
            entity.HasIndex(c => c.Identificacion).IsUnique();
        });
        
        builder.Entity<Vehiculo>(entity =>
        {
            entity.HasIndex(v => v.Placa).IsUnique();
        });
        
        builder.Entity<EventoCiudadano>(entity =>
        {
            entity.HasKey(ec => new { ec.EventoId, ec.CiudadanoId });
            entity.HasOne(ec => ec.Evento)
                .WithMany(e => e.Ciudadanos)
                .HasForeignKey(ec => ec.EventoId);
            entity.HasOne(ec => ec.Ciudadano)
                .WithMany()
                .HasForeignKey(ec => ec.CiudadanoId);
            entity.HasOne(ec => ec.Vehiculo)
                .WithMany()
                .HasForeignKey(ec => ec.VehiculoId);
        });
        
        builder.Entity<EventoUnidad>(entity =>
        {
            entity.HasKey(eu => new { eu.EventoId, eu.UnidadId });
            entity.HasOne(eu => eu.Evento)
                .WithMany(e => e.Unidades)
                .HasForeignKey(eu => eu.EventoId);
            entity.HasOne(eu => eu.Unidad)
                .WithMany()
                .HasForeignKey(eu => eu.UnidadId);
            entity.HasOne(eu => eu.Agente)
                .WithMany()
                .HasForeignKey(eu => eu.AgenteId);
            // Nivel y denominación quedan como foto del momento del evento (las estadísticas
            // no cambian si luego se reasigna la denominación o se cambia su nivel)
            entity.HasOne(eu => eu.NivelDenominacion)
                .WithMany()
                .HasForeignKey(eu => eu.NivelDenominacionId);
            entity.HasOne(eu => eu.Denominacion)
                .WithMany()
                .HasForeignKey(eu => eu.DenominacionId);
            entity.HasIndex(eu => new { eu.DenominacionId, eu.EventoId });
        });

        builder.Entity<EventoTipoEvento>(entity =>
        {
            entity.HasKey(et => new { et.EventoId, et.TipoEventoId });
            entity.HasOne(et => et.Evento)
                .WithMany(e => e.Tipos)
                .HasForeignKey(et => et.EventoId);
            entity.HasOne(et => et.TipoEvento)
                .WithMany()
                .HasForeignKey(et => et.TipoEventoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Evento>(entity => entity.HasIndex(e => e.CreatedAt));
        
        builder.Entity<Unidad>(entity =>
        {
            entity.Property(u => u.Ficha).HasMaxLength(Unidad.FichaMaxLength);
            entity.Property(u => u.Placa).HasMaxLength(Unidad.PlacaMaxLength);
            entity.HasIndex(u => u.Ficha).IsUnique();
            entity.HasIndex(u => u.DenominacionId);
            entity.HasOne(u => u.Denominacion)
                .WithMany()
                .HasForeignKey(u => u.DenominacionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<HistorialDenominacionUnidad>(entity =>
        {
            // Registro de auditoría: nada en cascada, las referencias no pueden borrarse mientras exista el historial
            entity.HasOne<Unidad>()
                .WithMany(u => u.HistorialDenominaciones)
                .HasForeignKey(h => h.UnidadId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(h => h.UnidadRelacionada)
                .WithMany()
                .HasForeignKey(h => h.UnidadRelacionadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(h => h.DenominacionAnterior)
                .WithMany()
                .HasForeignKey(h => h.DenominacionAnteriorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(h => h.DenominacionNueva)
                .WithMany()
                .HasForeignKey(h => h.DenominacionNuevaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(h => h.Observacion).HasMaxLength(AutorCambio.ObservacionMaxLength);
            entity.HasIndex(h => new { h.UnidadId, h.FechaUtc });
            entity.HasIndex(h => h.DenominacionAnteriorId);
            entity.HasIndex(h => h.DenominacionNuevaId);
        });

        builder.Entity<Denominacion>(entity =>
        {
            entity.Property(d => d.Nombre).HasMaxLength(Denominacion.NombreMaxLength);
            entity.HasIndex(d => d.Nombre).IsUnique();
            entity.HasOne(d => d.Tramo)
                .WithMany()
                .HasForeignKey(d => d.TramoId);
            entity.HasOne(d => d.Nivel)
                .WithMany()
                .HasForeignKey(d => d.NivelDenominacionId);

            // Asignaciones: forman parte del agregado; al quitarlas de la colección se eliminan
            entity.HasMany(d => d.Regiones)
                .WithOne()
                .HasForeignKey(r => r.DenominacionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(d => d.Tramos)
                .WithOne()
                .HasForeignKey(t => t.DenominacionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DenominacionRegion>(entity =>
        {
            entity.HasIndex(r => new { r.DenominacionId, r.RegionMacro })
                .IsUnique()
                .HasFilter("[RegionMacro] IS NOT NULL");
            entity.HasIndex(r => new { r.DenominacionId, r.RegionAsistenciaId })
                .IsUnique()
                .HasFilter("[RegionAsistenciaId] IS NOT NULL");
            entity.HasOne(r => r.RegionAsistencia)
                .WithMany()
                .HasForeignKey(r => r.RegionAsistenciaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_denominacion_regiones_macro_o_asistencia",
                "([RegionMacro] IS NOT NULL AND [RegionAsistenciaId] IS NULL) OR ([RegionMacro] IS NULL AND [RegionAsistenciaId] IS NOT NULL)"));
        });

        builder.Entity<DenominacionTramo>(entity =>
        {
            entity.HasKey(t => new { t.DenominacionId, t.TramoId });
            entity.HasOne(t => t.Tramo)
                .WithMany()
                .HasForeignKey(t => t.TramoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        builder.Entity<Flota>(entity =>
        {
            entity.HasIndex(f => new { f.Numero, f.Codigo }).IsUnique();
            entity.HasOne(f => f.Denominacion)
                .WithMany()
                .HasForeignKey(f => f.DenominacionId);
        });

        #endregion
    }

    #region Entities

    // Misc
    public DbSet<TipoVehiculo> TipoVehiculos { get; set; }
    public DbSet<Domain.Entities.Misc.Color> Colores { get; set; }
    public DbSet<Marca> Marcas { get; set; }
    public DbSet<Modelo> Modelos { get; set; }
    public DbSet<Provincia> Provincias { get; set; }
    public DbSet<Municipio> Municipios { get; set; }
    public DbSet<Nacionalidad> Nacionalidades { get; set; }
    public DbSet<Rango> Rangos { get; set; }
    public DbSet<Departamento> Departamentos { get; set; }
    
    // Historico
    public DbSet<Ciudadano> Ciudadanos { get; set; }
    public DbSet<Vehiculo>  Vehiculos { get; set; }
    
    // Operaciones
    public DbSet<TipoEvento> TipoEventos { get; set; }
    public DbSet<Evento> Eventos { get; set; }
    public DbSet<EventoCiudadano> EventoCiudadanos { get; set; }
    public DbSet<EventoUnidad> EventoUnidades { get; set; }
    public DbSet<Agente> Agentes { get; set; }
    public DbSet<NivelDenominacion> NivelesDenominacion { get; set; }
    public DbSet<Unidad> Unidades { get; set; }
    public DbSet<Denominacion> Denominaciones { get; set; }
    public DbSet<DenominacionRegion> DenominacionRegiones { get; set; }
    public DbSet<DenominacionTramo> DenominacionTramos { get; set; }
    public DbSet<EventoTipoEvento> EventoTiposEvento { get; set; }
    public DbSet<HistorialDenominacionUnidad> HistorialDenominaciones { get; set; }
    public DbSet<Flota> Flotas { get; set; }
    public DbSet<Tramo> Tramos { get; set; }
    public DbSet<RegionAsistencia> Regiones { get; set; }

    #endregion
    
}