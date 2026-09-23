using Application.Common.Models;
using Application.Contracts.Operaciones;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

// Query: búsqueda paginada por ficha, placa o denominación
public record GetUnidadesQuery(int Page = 1, int Size = 10, string? SearchTerm = null)
    : IRequest<PagedResult<UnidadViewModel>>, IPagedQuery;

// Validator
public class GetUnidadesQueryValidator : PagedQueryValidator<GetUnidadesQuery>;

// Handler
public class GetUnidadesQueryHandler(IUnidadQueries queries)
    : IRequestHandler<GetUnidadesQuery, PagedResult<UnidadViewModel>>
{
    public Task<PagedResult<UnidadViewModel>> Handle(GetUnidadesQuery request, CancellationToken cancellationToken)
        => queries.GetPagedAsync(request.Page, request.Size, request.SearchTerm, cancellationToken);
}
