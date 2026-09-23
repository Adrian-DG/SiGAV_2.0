using Domain.Entities.Operaciones;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class AgenteRepository(SiGAVContext context) : Repository(context), IAgenteRepository
{
    public Task<Agente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Agentes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    // Rango se incluye porque Agente.GetRango / GetInfo lo necesitan
    public Task<Agente?> GetActivoByIdentificacionAsync(string identificacion, CancellationToken cancellationToken = default)
        => _context.Agentes
            .AsNoTracking()
            .Include(a => a.Rango)
            .FirstOrDefaultAsync(a => a.Identificacion == identificacion && a.IsActive, cancellationToken);

    public Task<bool> ExisteIdentificacionAsync(string identificacion, int? excluirAgenteId = null, CancellationToken cancellationToken = default)
        => _context.Agentes.AnyAsync(a => a.Identificacion == identificacion && (excluirAgenteId == null || a.Id != excluirAgenteId), cancellationToken);

    public void Add(Agente agente) => _context.Agentes.Add(agente);
}
