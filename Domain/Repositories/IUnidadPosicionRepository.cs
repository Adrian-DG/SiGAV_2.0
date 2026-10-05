using Domain.Entities.Operaciones;

namespace Domain.Repositories;

public interface IUnidadPosicionRepository
{
    Task<UnidadPosicion?> GetByUnidadIdAsync(int unidadId, CancellationToken cancellationToken = default);
    void Add(UnidadPosicion posicion);
}
