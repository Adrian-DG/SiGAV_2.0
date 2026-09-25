using Domain.Entities.Historico;
using Domain.Entities.Misc;
using Domain.Entities.Operaciones;

namespace Application.Contracts;

/// <summary>
/// Lado de lectura de CQRS: las Queries consultan y proyectan directamente sobre estos conjuntos
/// (sin seguimiento de cambios, así que nada de lo que se lea aquí puede guardarse).
/// Los Commands no lo usan para modificar datos: cargan agregados con los repositorios del
/// dominio y confirman con <see cref="Domain.Repositories.IUnitOfWork"/>.
/// </summary>
public interface IReadDbContext
{
    // Misc
    IQueryable<Provincia> Provincias { get; }
    IQueryable<Municipio> Municipios { get; }
    IQueryable<Nacionalidad> Nacionalidades { get; }
    IQueryable<Rango> Rangos { get; }
    IQueryable<TipoVehiculo> TiposVehiculo { get; }
    IQueryable<Marca> Marcas { get; }
    IQueryable<Modelo> Modelos { get; }
    IQueryable<Color> Colores { get; }

    // Histórico
    IQueryable<Ciudadano> Ciudadanos { get; }
    IQueryable<Vehiculo> Vehiculos { get; }

    // Operaciones
    IQueryable<RegionAsistencia> Regiones { get; }
    IQueryable<Tramo> Tramos { get; }
    IQueryable<NivelDenominacion> NivelesDenominacion { get; }
    IQueryable<Denominacion> Denominaciones { get; }
    IQueryable<Unidad> Unidades { get; }
    IQueryable<HistorialDenominacionUnidad> HistorialDenominaciones { get; }
    IQueryable<Agente> Agentes { get; }
    IQueryable<TipoEvento> TiposEvento { get; }
    IQueryable<Evento> Eventos { get; }
    IQueryable<EventoTipoEvento> EventoTiposEvento { get; }
    IQueryable<EventoUnidadInfo> EventoUnidades { get; }
    IQueryable<EventoCiudadanoInfo> EventoCiudadanos { get; }

    /// <summary>Usuarios de la web (la entidad de Identity vive en Infrastructure).</summary>
    IQueryable<UsuarioLectura> Usuarios { get; }
}

/// <summary>Propiedades init (no constructor): EF solo puede componer joins sobre proyecciones de miembros.</summary>
public sealed record UsuarioLectura
{
    public int Id { get; init; }
    public string? UserName { get; init; }
}
