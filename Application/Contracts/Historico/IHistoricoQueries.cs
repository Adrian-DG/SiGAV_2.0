using Application.Features.Historico;

namespace Application.Contracts.Historico;

public interface IHistoricoQueries
{
    /// <summary>Último registro de la persona en un evento; si no hay, el maestro histórico. null si no se conoce.</summary>
    Task<CiudadanoConocidoViewModel?> BuscarCiudadanoAsync(string identificacionNormalizada, CancellationToken cancellationToken = default);

    /// <summary>Último registro del vehículo en un evento; si no hay, el maestro histórico. null si no se conoce.</summary>
    Task<VehiculoConocidoViewModel?> BuscarVehiculoAsync(string placaNormalizada, CancellationToken cancellationToken = default);
}
