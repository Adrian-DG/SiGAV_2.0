using Domain.Entities.Historico;

namespace Domain.Repositories;

/// <summary>Maestros históricos de ciudadanos y vehículos (p. ej. importados de SiGAV 1.0).</summary>
public interface IHistoricoRepository
{
    Task<Ciudadano?> GetCiudadanoAsync(string identificacion, CancellationToken cancellationToken = default);
    Task<Vehiculo?> GetVehiculoAsync(string placa, CancellationToken cancellationToken = default);
}
