using Domain.Entities.Operaciones;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class DenominacionRepository(SiGAVContext context) : Repository(context), IDenominacionRepository
{
    public Task<Denominacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Denominaciones
            .Include(d => d.Nivel)
            .Include(d => d.Regiones)
            .Include(d => d.Tramos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);

    public Task<NivelDenominacion?> GetNivelAsync(int nivelDenominacionId, CancellationToken cancellationToken = default)
        => _context.NivelesDenominacion.FirstOrDefaultAsync(n => n.Id == nivelDenominacionId && n.IsActive, cancellationToken);

    public Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => _context.Denominaciones.AnyAsync(d => d.Nombre == nombre, cancellationToken);

    public void Add(Denominacion denominacion) => _context.Denominaciones.Add(denominacion);
}
