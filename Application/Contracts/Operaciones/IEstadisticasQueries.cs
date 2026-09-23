using Application.Features.Estadisticas;
using Domain.ValueObjects;

namespace Application.Contracts.Operaciones;

public interface IEstadisticasQueries
{
    Task<EstadisticasEventosViewModel> GetEstadisticasEventosAsync(
        AlcanceEstadistico alcance,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default);
}
