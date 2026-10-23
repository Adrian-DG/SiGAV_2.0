using Domain.Entities.Operaciones;

namespace Domain.Repositories;

public interface IUnidadRepository
{
    Task<Unidad?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Unidad con su denominación cargada (requerida para registrarla en un evento).</summary>
    Task<Unidad?> GetConDenominacionAsync(int id, CancellationToken cancellationToken = default);
    Task<Unidad?> GetByFichaAsync(string ficha, CancellationToken cancellationToken = default);
    Task<bool> ExisteFichaAsync(string ficha, int? excluirUnidadId = null, CancellationToken cancellationToken = default);
    Task<List<Unidad>> GetActivasConDenominacionAsync(int denominacionId, CancellationToken cancellationToken = default);
    /// <summary>Unidades (activas o no) cuyas fichas están en la lista, sin distinguir mayúsculas.</summary>
    Task<List<Unidad>> GetByFichasAsync(IReadOnlyCollection<string> fichas, CancellationToken cancellationToken = default);
    /// <summary>Unidades activas que tienen alguna de las denominaciones.</summary>
    Task<List<Unidad>> GetActivasConDenominacionesAsync(IReadOnlyCollection<int> denominacionIds, CancellationToken cancellationToken = default);
    void Add(Unidad unidad);
}
