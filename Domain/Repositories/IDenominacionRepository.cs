using Domain.Entities.Operaciones;

namespace Domain.Repositories;

public interface IDenominacionRepository
{
    Task<Denominacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default);
    void Add(Denominacion denominacion);
}
