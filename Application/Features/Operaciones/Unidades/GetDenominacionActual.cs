using Application.Contracts.Operaciones;
using Application.Exceptions;
using Domain.ViewModels;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

// Query
public record GetDenominacionActualQuery(int UnidadId) : IRequest<NamedViewModel>;

// Handler
public class GetDenominacionActualQueryHandler(IUnidadQueries queries) : IRequestHandler<GetDenominacionActualQuery, NamedViewModel>
{
    public async Task<NamedViewModel> Handle(GetDenominacionActualQuery request, CancellationToken cancellationToken)
    {
        // En SiGAV 1.0 esto lanzaba NullReferenceException si la unidad no existía o no tenía denominación
        return await queries.GetDenominacionActualAsync(request.UnidadId, cancellationToken)
            ?? throw new NotFoundException($"La unidad '{request.UnidadId}' no existe o no tiene denominación asignada.");
    }
}
