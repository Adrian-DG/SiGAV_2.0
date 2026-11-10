using Domain.Entities.Operaciones;

namespace Domain.Repositories;

/// <summary>Zonas y sus tramos (lado de escritura).</summary>
public interface IZonaRepository
{
    /// <summary>Zona activa.</summary>
    Task<Zona?> GetZonaAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Entre todas las zonas (activas o no), sin distinguir mayúsculas.</summary>
    Task<bool> ExisteNombreZonaAsync(string nombre, CancellationToken cancellationToken = default);
    /// <summary>
    /// Entre todos los tramos (activos o no) de cualquier zona, sin distinguir mayúsculas: la carga
    /// masiva de unidades identifica el tramo por su nombre.
    /// </summary>
    Task<bool> ExisteNombreTramoAsync(string nombre, CancellationToken cancellationToken = default);
    void Add(Zona zona);
    void Add(Tramo tramo);
}
