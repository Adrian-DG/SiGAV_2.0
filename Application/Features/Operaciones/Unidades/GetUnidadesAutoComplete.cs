using Application.Common;
using Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Unidades;

// Query: sugerencias de unidades activas por ficha o denominación
public record GetUnidadesAutoCompleteQuery(string? Term, bool EsAmbulancia) : IRequest<IReadOnlyList<UnidadAutoCompleteViewModel>>;

// Handler
public class GetUnidadesAutoCompleteQueryHandler(IReadDbContext db)
    : IRequestHandler<GetUnidadesAutoCompleteQuery, IReadOnlyList<UnidadAutoCompleteViewModel>>
{
    private const int MaxResultados = 10;

    public async Task<IReadOnlyList<UnidadAutoCompleteViewModel>> Handle(GetUnidadesAutoCompleteQuery request, CancellationToken cancellationToken)
    {
        var patron = QueryableExtensions.PatronBusqueda(request.Term) ?? "%";

        var query = db.Unidades
            .Where(u => u.IsActive)
            .Where(u => EF.Functions.Like(u.Ficha, patron)
                || (u.Denominacion != null && EF.Functions.Like(u.Denominacion.Nombre, patron)));

        query = request.EsAmbulancia
            ? query.Where(u => u.Denominacion != null && u.Denominacion.Nivel!.EsAmbulancia)
            : query.Where(u => u.Denominacion == null || !u.Denominacion.Nivel!.EsAmbulancia);

        return await query
            .OrderBy(u => u.Ficha)
            .Take(MaxResultados)
            .Select(u => new UnidadAutoCompleteViewModel(
                u.Id,
                u.Ficha,
                u.Placa,
                u.Denominacion != null ? u.Denominacion.Nombre : "Sin Denominación",
                u.Denominacion != null ? u.Denominacion.Tramo!.Nombre : "No Disponible",
                u.EstaDisponible))
            .ToListAsync(cancellationToken);
    }
}
