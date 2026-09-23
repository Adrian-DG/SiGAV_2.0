using Application.Common.Models;
using Application.Features.Operaciones.Agentes;
using Domain.Enums;

namespace Application.Contracts.Operaciones;

public interface IAgenteQueries
{
    Task<PagedResult<AgenteViewModel>> GetPagedAsync(
        int page,
        int size,
        string? searchTerm,
        bool? autorizado,
        AreaOperativaEnum? areaOperativa,
        bool incluirInactivos,
        CancellationToken cancellationToken = default);

    Task<AgenteViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ConfirmAgenteViewModel> ConfirmarAsync(string identificacion, CancellationToken cancellationToken = default);

    /// <summary>Solo agentes activos y autorizados.</summary>
    Task<IReadOnlyList<AgenteAutoCompleteViewModel>> AutoCompleteAsync(string term, AreaOperativaEnum? areaOperativa, int take, CancellationToken cancellationToken = default);
}
