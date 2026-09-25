using Domain.Entities.Operaciones;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class EventoRepository(SiGAVContext context) : Repository(context), IEventoRepository
{
    public Task<Evento?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Eventos
            .Include(e => e.Unidades)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<int?> GetIdByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
        => _context.Eventos
            .Where(e => e.RequestId == requestId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(Evento evento) => _context.Eventos.Add(evento);
}
