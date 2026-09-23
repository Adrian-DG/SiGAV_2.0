using Application.Common.Models;
using Application.Features.Operaciones.Unidades;
using Domain.ViewModels;

namespace Application.Contracts.Operaciones;

public interface IUnidadQueries
{
    Task<PagedResult<UnidadViewModel>> GetPagedAsync(int page, int size, string? searchTerm, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnidadAutoCompleteViewModel>> AutoCompleteAsync(string term, bool esAmbulancia, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnidadViewModel>> GetByTramoAsync(int tramoId, CancellationToken cancellationToken = default);
    Task<bool> ExisteActivaYDisponibleAsync(string ficha, CancellationToken cancellationToken = default);
    Task<bool> EstaDisponibleAsync(string ficha, CancellationToken cancellationToken = default);
    Task<NamedViewModel?> GetDenominacionActualAsync(int unidadId, CancellationToken cancellationToken = default);
}
