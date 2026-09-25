using Application.Common;
using Application.Common.Models;
using Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Denominaciones;

// Query
public record GetDenominacionesQuery(int Page = 1, int Size = 10, string? SearchTerm = null)
    : IRequest<PagedResult<DenominacionViewModel>>, IPagedQuery;

// Validator
public class GetDenominacionesQueryValidator : PagedQueryValidator<GetDenominacionesQuery>;

// Handler
public class GetDenominacionesQueryHandler(IReadDbContext db)
    : IRequestHandler<GetDenominacionesQuery, PagedResult<DenominacionViewModel>>
{
    public Task<PagedResult<DenominacionViewModel>> Handle(GetDenominacionesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Denominaciones.Where(d => d.IsActive);

        if (QueryableExtensions.PatronBusqueda(request.SearchTerm) is { } patron)
            query = query.Where(d => EF.Functions.Like(d.Nombre, patron));

        return query
            .OrderBy(d => d.Nivel!.Jerarquia)
            .ThenBy(d => d.Nombre)
            .Select(d => new DenominacionViewModel(
                d.Id,
                d.Nombre,
                d.NivelDenominacionId,
                d.Nivel!.Nombre,
                d.Nivel.Jerarquia,
                d.TramoId,
                d.Tramo!.Nombre))
            .ToPagedResultAsync(request.Page, request.Size, cancellationToken);
    }
}
