using Domain.Entities.Operaciones;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class UnidadPosicionRepository(SiGAVContext context) : IUnidadPosicionRepository
{
    public Task<UnidadPosicion?> GetByUnidadIdAsync(int unidadId, CancellationToken cancellationToken = default)
        => context.UnidadPosiciones.FirstOrDefaultAsync(p => p.UnidadId == unidadId, cancellationToken);

    public void Add(UnidadPosicion posicion) => context.UnidadPosiciones.Add(posicion);
}
