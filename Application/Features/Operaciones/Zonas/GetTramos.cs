using Application.Common;
using Application.Contracts;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Zonas;

// Query: tramos de una zona o región, o por nombre; ordenados por región, zona y nombre. Sin paginar: son pocos.
public record GetTramosQuery(
    int? ZonaId = null,
    RegionMacroEnum? Region = null,
    string? SearchTerm = null,
    bool IncluirInactivos = false) : IRequest<List<TramoViewModel>>;

// Validator
public class GetTramosQueryValidator : AbstractValidator<GetTramosQuery>
{
    public GetTramosQueryValidator()
    {
        RuleFor(x => x.Region).IsInEnum().WithMessage("La región no es válida.");
        RuleFor(x => x.SearchTerm).MaximumLength(100).WithMessage("El término de búsqueda no puede exceder 100 caracteres.");
    }
}

// Handler
public class GetTramosQueryHandler(IReadDbContext db) : IRequestHandler<GetTramosQuery, List<TramoViewModel>>
{
    public Task<List<TramoViewModel>> Handle(GetTramosQuery request, CancellationToken cancellationToken)
    {
        var query = db.Tramos.AsQueryable();
        if (!request.IncluirInactivos) query = query.Where(t => t.IsActive && t.Zona!.IsActive);
        if (request.ZonaId is { } zonaId) query = query.Where(t => t.ZonaId == zonaId);
        if (request.Region is { } region) query = query.Where(t => t.Zona!.Region == region);
        if (QueryableExtensions.PatronBusqueda(request.SearchTerm) is { } patron)
            query = query.Where(t => EF.Functions.Like(t.Nombre, patron));

        return query
            .OrderBy(t => t.Zona!.Region)
            .ThenBy(t => t.Zona!.Nombre)
            .ThenBy(t => t.Nombre)
            .Select(t => new TramoViewModel(
                t.Id,
                t.Nombre,
                t.ZonaId,
                t.Zona!.Nombre,
                t.Zona.Region,
                t.IsActive,
                db.Denominaciones.Count(d => d.IsActive && d.TramoId == t.Id)))
            .ToListAsync(cancellationToken);
    }
}
