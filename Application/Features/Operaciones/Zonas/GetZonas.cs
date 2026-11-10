using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Zonas;

// Query: zonas (de una región o de todas) con sus tramos. Son pocas: se devuelven todas, sin paginar.
public record GetZonasQuery(RegionMacroEnum? Region = null, bool IncluirInactivos = false) : IRequest<List<ZonaViewModel>>;

// Query: una zona con todos sus tramos (también los inactivos)
public record GetZonaQuery(int ZonaId) : IRequest<ZonaViewModel>;

// Validator
public class GetZonasQueryValidator : AbstractValidator<GetZonasQuery>
{
    public GetZonasQueryValidator()
    {
        RuleFor(x => x.Region).IsInEnum().WithMessage("La región no es válida.");
    }
}

// Handlers
public class GetZonasQueryHandler(IReadDbContext db) : IRequestHandler<GetZonasQuery, List<ZonaViewModel>>
{
    public Task<List<ZonaViewModel>> Handle(GetZonasQuery request, CancellationToken cancellationToken)
    {
        var query = db.Zonas.AsQueryable();
        if (!request.IncluirInactivos) query = query.Where(z => z.IsActive);
        if (request.Region is { } region) query = query.Where(z => z.Region == region);

        return ZonaProyeccion.Proyectar(db, query.OrderBy(z => z.Region).ThenBy(z => z.Nombre), request.IncluirInactivos)
            .ToListAsync(cancellationToken);
    }
}

public class GetZonaQueryHandler(IReadDbContext db) : IRequestHandler<GetZonaQuery, ZonaViewModel>
{
    public async Task<ZonaViewModel> Handle(GetZonaQuery request, CancellationToken cancellationToken)
        => await ZonaProyeccion.Proyectar(db, db.Zonas.Where(z => z.Id == request.ZonaId), incluirTramosInactivos: true)
               .FirstOrDefaultAsync(cancellationToken)
           ?? throw new NotFoundException("La zona", request.ZonaId);
}

internal static class ZonaProyeccion
{
    public static IQueryable<ZonaViewModel> Proyectar(IReadDbContext db, IQueryable<Zona> zonas, bool incluirTramosInactivos)
        => zonas.Select(z => new ZonaViewModel(
            z.Id,
            z.Nombre,
            z.Region,
            z.IsActive,
            db.Tramos
                .Where(t => t.ZonaId == z.Id && (incluirTramosInactivos || t.IsActive))
                .OrderBy(t => t.Nombre)
                .Select(t => new TramoDeZonaViewModel(
                    t.Id,
                    t.Nombre,
                    t.IsActive,
                    db.Denominaciones.Count(d => d.IsActive && d.TramoId == t.Id)))
                .ToList()));
}
