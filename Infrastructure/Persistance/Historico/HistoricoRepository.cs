using Domain.Entities.Historico;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Historico;

public class HistoricoRepository(SiGAVContext context) : Repository(context), IHistoricoRepository
{
    public Task<Ciudadano?> GetCiudadanoAsync(string identificacion, CancellationToken cancellationToken = default)
        => _context.Ciudadanos.FirstOrDefaultAsync(c => c.Identificacion == identificacion && c.IsActive, cancellationToken);

    public Task<Vehiculo?> GetVehiculoAsync(string placa, CancellationToken cancellationToken = default)
        => _context.Vehiculos.FirstOrDefaultAsync(v => v.Placa == placa && v.IsActive, cancellationToken);
}
