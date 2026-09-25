using Application.Common;
using Application.Common.Models;
using Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Unidades;

// Query: búsqueda paginada por ficha, placa o denominación
public record GetUnidadesQuery(int Page = 1, int Size = 10, string? SearchTerm = null)
    : IRequest<PagedResult<UnidadViewModel>>, IPagedQuery;

// Validator
public class GetUnidadesQueryValidator : PagedQueryValidator<GetUnidadesQuery>;

// Handler
public class GetUnidadesQueryHandler(IReadDbContext db)
    : IRequestHandler<GetUnidadesQuery, PagedResult<UnidadViewModel>>
{
    public Task<PagedResult<UnidadViewModel>> Handle(GetUnidadesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Unidades;

        if (QueryableExtensions.PatronBusqueda(request.SearchTerm) is { } patron)
            query = query.Where(u => EF.Functions.Like(u.Ficha, patron)
                || (u.Placa != null && EF.Functions.Like(u.Placa, patron))
                || (u.Denominacion != null && EF.Functions.Like(u.Denominacion.Nombre, patron)));

        // Encargados primero; las unidades sin denominación al final
        return query
            .OrderBy(u => u.Denominacion != null ? (int)u.Denominacion.Nivel!.Jerarquia : int.MaxValue)
            .ThenBy(u => u.Ficha)
            .Select(UnidadViewModel.Proyeccion)
            .ToPagedResultAsync(request.Page, request.Size, cancellationToken);
    }
}
