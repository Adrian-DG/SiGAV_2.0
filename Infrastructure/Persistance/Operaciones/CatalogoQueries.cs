using Application.Contracts.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class CatalogoQueries(SiGAVContext context) : ICatalogoQueries
{
    public Task<bool> ExisteTramoAsync(int tramoId, CancellationToken cancellationToken = default)
        => context.Tramos.AnyAsync(t => t.Id == tramoId && t.IsActive, cancellationToken);

    public Task<bool> ExisteTipoUnidadAsync(int tipoUnidadId, CancellationToken cancellationToken = default)
        => context.TipoUnidades.AnyAsync(t => t.Id == tipoUnidadId && t.IsActive, cancellationToken);
}
