using Domain.Entities.Operaciones;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class AgenteRepository(SiGAVContext context) : Repository(context), IAgenteRepository
{
    // Rango se incluye porque Agente.GetRango / GetInfo lo necesitan
    public Task<Agente?> GetActivoByIdentificacionAsync(string identificacion, CancellationToken cancellationToken = default)
        => _context.Agentes
            .AsNoTracking()
            .Include(a => a.Rango)
            .FirstOrDefaultAsync(a => a.Identificacion == identificacion && a.IsActive, cancellationToken);
}
