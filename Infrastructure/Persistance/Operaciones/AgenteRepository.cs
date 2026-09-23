using Domain.Entities.Operaciones;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class AgenteRepository(SiGAVContext context) : Repository(context), IAgenteRepository
{
    public Task<Agente?> GetActivoByIdentificacionAsync(string identificacion, CancellationToken cancellationToken = default)
        => _context.Agentes.AsNoTracking().FirstOrDefaultAsync(a => a.Identificacion == identificacion && a.IsActive, cancellationToken);
}
