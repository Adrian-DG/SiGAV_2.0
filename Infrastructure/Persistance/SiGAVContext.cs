using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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

        RestringirBorradoEnCascada(builder);
    }

    /// <summary>
    /// Todas las fechas con hora del modelo se guardan en UTC. Los proveedores (SQLite en
    /// particular) las devuelven con Kind = Unspecified; aquí se marcan como UTC al leer para que
    /// se serialicen con "Z" y no se interpreten como hora local.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.ToUniversalTime(), DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    /// <summary>
    /// Borrar un catálogo o maestro del dominio (provincia, agente, unidad, color...) nunca debe
    /// arrastrar datos operativos: por convención EF pone Cascade en toda FK requerida. Aquí se
    /// cambia a Restrict salvo que la configuración lo declare explícitamente (colecciones propias
    /// de un agregado, como las unidades de un evento). Evita además las rutas de cascada múltiples
    /// que SQL Server rechaza.
    /// </summary>
    private static void RestringirBorradoEnCascada(ModelBuilder builder)
    {
        var foreignKeys = builder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => !fk.IsOwnership
                && fk.DeleteBehavior == DeleteBehavior.Cascade
                && fk.PrincipalEntityType.ClrType.Namespace?.StartsWith("Domain.", StringComparison.Ordinal) == true
                && ((IConventionForeignKey)fk).GetDeleteBehaviorConfigurationSource() != ConfigurationSource.Explicit);

        foreach (var foreignKey in foreignKeys.ToList())
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
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
    public DbSet<EventoEvidencia> EventoEvidencias { get; set; }
    public DbSet<HistorialDenominacionUnidad> HistorialDenominaciones { get; set; }
    public DbSet<Flota> Flotas { get; set; }
    public DbSet<Tramo> Tramos { get; set; }
    public DbSet<RegionAsistencia> Regiones { get; set; }

    #endregion
    
}