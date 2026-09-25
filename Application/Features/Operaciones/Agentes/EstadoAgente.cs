using Application.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.Features.Operaciones.Agentes;

#region Autorizacion

// Command: autoriza o revoca el acceso a la app (en SiGAV 1.0 era un toggle con "Type = 1")
public record CambiarAutorizacionAgenteCommand(int AgenteId, bool Autorizado) : IRequest;

public class CambiarAutorizacionAgenteCommandHandler(IAgenteRepository agentes, IUnitOfWork uow)
    : IRequestHandler<CambiarAutorizacionAgenteCommand, Unit>
{
    public async Task<Unit> Handle(CambiarAutorizacionAgenteCommand request, CancellationToken cancellationToken)
    {
        var agente = await agentes.GetByIdAsync(request.AgenteId, cancellationToken)
            ?? throw new NotFoundException("El agente", request.AgenteId);

        if (request.Autorizado) agente.Autorizar();
        else agente.RevocarAutorizacion();

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

#endregion

#region Desactivar

// Command: baja lógica del agente
public record DesactivarAgenteCommand(int AgenteId) : IRequest;

public class DesactivarAgenteCommandHandler(IAgenteRepository agentes, IUnitOfWork uow)
    : IRequestHandler<DesactivarAgenteCommand, Unit>
{
    public async Task<Unit> Handle(DesactivarAgenteCommand request, CancellationToken cancellationToken)
    {
        var agente = await agentes.GetByIdAsync(request.AgenteId, cancellationToken)
            ?? throw new NotFoundException("El agente", request.AgenteId);

        agente.Desactivar();

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}


#endregion