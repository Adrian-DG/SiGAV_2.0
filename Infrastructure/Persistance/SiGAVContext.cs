using System.Drawing;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Entities.Misc;
using Domain.Entities.Historico;
using Domain.Entities.Operaciones;

namespace Infrastructure.Persistance;

public class SiGAVContext(DbContextOptions<SiGAVContext> options) : IdentityDbContext<AppUser, AppPermission, int>(options)
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
            entity.HasOne(eu => eu.TipoUnidad)
                .WithMany()
                .HasForeignKey(eu => eu.TipoUnidadId);
            entity.HasOne(eu => eu.Denominacion)
                .WithMany()
                .HasForeignKey(eu => eu.DenominacionId);
        });
        
        builder.Entity<Unidad>(entity =>
        {
            entity.HasIndex(u => new { u.Ficha, u.Placa }).IsUnique();
            entity.HasOne(u => u.Denominacion)
                .WithMany()
                .HasForeignKey(u => u.DenominacionId);
        });
        
        builder.Entity<Denominacion>(entity =>
        {
            entity.HasOne(d => d.Tramo)
                .WithMany()
                .HasForeignKey(d => d.TramoId);
            entity.HasOne(d => d.TipoUnidad)
                .WithMany()
                .HasForeignKey(d => d.TipoUnidadId);
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
    public DbSet<TipoUnidad> TipoUnidades { get; set; }
    public DbSet<Unidad> Unidades { get; set; }
    public DbSet<Denominacion> Denominaciones { get; set; }
    public DbSet<Flota> Flotas { get; set; }
    public DbSet<Tramo> Tramos { get; set; }
    public DbSet<RegionAsistencia> Regiones { get; set; }

    #endregion
    
}