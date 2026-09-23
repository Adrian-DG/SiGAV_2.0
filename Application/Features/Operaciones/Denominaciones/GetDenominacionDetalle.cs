using Application.Contracts.Operaciones;
using Application.Exceptions;
using MediatR;

namespace Application.Features.Operaciones.Denominaciones;

// Query: denominación con su nivel, jerarquía y asignaciones de supervisión
public record GetDenominacionDetalleQuery(int DenominacionId) : IRequest<DenominacionDetalleViewModel>;

// Handler
public class GetDenominacionDetalleQueryHandler(IDenominacionQueries queries)
    : IRequestHandler<GetDenominacionDetalleQuery, DenominacionDetalleViewModel>
{
    public async Task<DenominacionDetalleViewModel> Handle(GetDenominacionDetalleQuery request, CancellationToken cancellationToken)
        => await queries.GetDetalleAsync(request.DenominacionId, cancellationToken)
            ?? throw new NotFoundException("La denominación", request.DenominacionId);
}
