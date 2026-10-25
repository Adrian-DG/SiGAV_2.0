using Domain.Entities.Historico;

namespace Domain.Repositories;

/// <summary>
/// Maestros históricos de ciudadanos y vehículos: importados de SiGAV 1.0 o creados al registrar
/// eventos.
/// </summary>
public interface IHistoricoRepository
{
    /// <summary>Por identificación normalizada, aunque esté desactivado (es única).</summary>
    Task<Ciudadano?> GetCiudadanoAsync(string identificacion, CancellationToken cancellationToken = default);

    /// <summary>Por placa normalizada, aunque esté desactivado (es única).</summary>
    Task<Vehiculo?> GetVehiculoAsync(string placa, CancellationToken cancellationToken = default);

    void Add(Ciudadano ciudadano);
    void Add(Vehiculo vehiculo);
}
