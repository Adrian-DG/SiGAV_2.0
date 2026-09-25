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
    void Add(Unidad unidad);
}
