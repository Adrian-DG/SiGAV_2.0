using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.Misc;
using Domain.Entities.Historico;
using Domain.Entities.Operaciones;
using Domain.Repositories;

namespace Infrastructure.Persistance;

public class SiGAVContext(DbContextOptions<SiGAVContext> options) : IdentityDbContext<AppUser, AppPermission, int>(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // El mapeo de cada entidad vive en Persistance/Configurations (IEntityTypeConfiguration<T>);
        // el dominio no conoce la base de datos.
        builder.ApplyConfigurationsFromAssembly(typeof(SiGAVContext).Assembly);
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