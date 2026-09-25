using Domain.Entities.Historico;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Historico;

public class HistoricoRepository(SiGAVContext context) : IHistoricoRepository
{
    public Task<Ciudadano?> GetCiudadanoAsync(string identificacion, CancellationToken cancellationToken = default)
        => context.Ciudadanos.FirstOrDefaultAsync(c => c.Identificacion == identificacion && c.IsActive, cancellationToken);

    public Task<Vehiculo?> GetVehiculoAsync(string placa, CancellationToken cancellationToken = default)
        => context.Vehiculos.FirstOrDefaultAsync(v => v.Placa == placa && v.IsActive, cancellationToken);
}
