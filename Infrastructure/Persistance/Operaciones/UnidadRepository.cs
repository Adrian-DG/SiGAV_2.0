using Domain.Entities.Operaciones;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class UnidadRepository(SiGAVContext context) : Repository(context), IUnidadRepository
{
    public Task<Unidad?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Unidades.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Unidad?> GetConDenominacionAsync(int id, CancellationToken cancellationToken = default)
        => _context.Unidades
            .Include(u => u.Denominacion)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Unidad?> GetByFichaAsync(string ficha, CancellationToken cancellationToken = default)
        => _context.Unidades.FirstOrDefaultAsync(u => u.Ficha == ficha, cancellationToken);

    public Task<bool> ExisteFichaAsync(string ficha, int? excluirUnidadId = null, CancellationToken cancellationToken = default)
        => _context.Unidades.AnyAsync(u => u.Ficha == ficha && (excluirUnidadId == null || u.Id != excluirUnidadId), cancellationToken);

    public Task<List<Unidad>> GetActivasConDenominacionAsync(int denominacionId, CancellationToken cancellationToken = default)
        => _context.Unidades.Where(u => u.DenominacionId == denominacionId && u.IsActive).ToListAsync(cancellationToken);

    public void Add(Unidad unidad) => _context.Unidades.Add(unidad);
}
