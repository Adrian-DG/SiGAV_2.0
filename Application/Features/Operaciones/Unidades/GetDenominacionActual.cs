using Application.Contracts;
using Application.Exceptions;
using Domain.ViewModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Unidades;

// Query
public record GetDenominacionActualQuery(int UnidadId) : IRequest<NamedViewModel>;

// Handler
public class GetDenominacionActualQueryHandler(IReadDbContext db) : IRequestHandler<GetDenominacionActualQuery, NamedViewModel>
{
    public async Task<NamedViewModel> Handle(GetDenominacionActualQuery request, CancellationToken cancellationToken)
    {
        // En SiGAV 1.0 esto lanzaba NullReferenceException si la unidad no existía o no tenía denominación
        return await db.Unidades
            .Where(u => u.Id == request.UnidadId && u.Denominacion != null)
            .Select(u => new NamedViewModel(u.Denominacion!.Id, u.Denominacion.Nombre))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"La unidad '{request.UnidadId}' no existe o no tiene denominación asignada.");
    }
}
