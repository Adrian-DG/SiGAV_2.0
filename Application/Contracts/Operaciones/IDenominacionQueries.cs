using Application.Common.Models;
using Application.Features.Operaciones.Denominaciones;

namespace Application.Contracts.Operaciones;

public interface IDenominacionQueries
{
    Task<PagedResult<DenominacionViewModel>> GetPagedAsync(int page, int size, string? searchTerm, CancellationToken cancellationToken = default);
    Task<DenominacionDetalleViewModel?> GetDetalleAsync(int denominacionId, CancellationToken cancellationToken = default);
}
