using Domain.Entities.Operaciones;

namespace Domain.Repositories;

public interface IAgenteRepository
{
    Task<Agente?> GetActivoByIdentificacionAsync(string identificacion, CancellationToken cancellationToken = default);
}
