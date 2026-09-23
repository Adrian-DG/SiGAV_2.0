using Application.Common.Models;
using Application.Features.Operaciones.Historial;

namespace Application.Contracts.Operaciones;

public interface IHistorialDenominacionQueries
{
    Task<PagedResult<HistorialDenominacionViewModel>> GetByUnidadAsync(int unidadId, int page, int size, CancellationToken cancellationToken = default);

    /// <summary>Cambios donde la denominación fue la anterior o la nueva.</summary>
    Task<PagedResult<HistorialDenominacionViewModel>> GetByDenominacionAsync(int denominacionId, int page, int size, CancellationToken cancellationToken = default);
}
