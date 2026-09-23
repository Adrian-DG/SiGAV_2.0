using Application.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

// Command: desactivación lógica, la unidad ya no podrá iniciar sesión ni ser reasignada
public record DesactivarUnidadCommand(int UnidadId) : IRequest;

// Handler
public class DesactivarUnidadCommandHandler(IUnidadRepository unidades, IUnitOfWork uow)
    : IRequestHandler<DesactivarUnidadCommand, Unit>
{
    public async Task<Unit> Handle(DesactivarUnidadCommand request, CancellationToken cancellationToken)
    {
        var unidad = await unidades.GetByIdAsync(request.UnidadId, cancellationToken)
            ?? throw new NotFoundException("La unidad", request.UnidadId);

        unidad.Desactivar();
        await uow.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
