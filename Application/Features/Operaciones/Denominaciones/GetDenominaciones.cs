using Application.Common.Models;
using Application.Contracts.Operaciones;
using MediatR;

namespace Application.Features.Operaciones.Denominaciones;

// Query
public record GetDenominacionesQuery(int Page = 1, int Size = 10, string? SearchTerm = null)
    : IRequest<PagedResult<DenominacionViewModel>>, IPagedQuery;

// Validator
public class GetDenominacionesQueryValidator : PagedQueryValidator<GetDenominacionesQuery>;

// Handler
public class GetDenominacionesQueryHandler(IDenominacionQueries queries)
    : IRequestHandler<GetDenominacionesQuery, PagedResult<DenominacionViewModel>>
{
    public Task<PagedResult<DenominacionViewModel>> Handle(GetDenominacionesQuery request, CancellationToken cancellationToken)
        => queries.GetPagedAsync(request.Page, request.Size, request.SearchTerm, cancellationToken);
}
