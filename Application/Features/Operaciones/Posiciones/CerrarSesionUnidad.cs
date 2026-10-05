using Application.Contracts;
using Application.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.Features.Operaciones.Posiciones;

/// <summary>
/// El agente cerró sesión en la app: la unidad deja de mostrarse en línea y otro agente puede
/// iniciar sesión con ella de inmediato (sin esperar a que se considere desconectada).
/// </summary>
public record CerrarSesionMovilCommand : IRequest<Unit>;

public class CerrarSesionMovilCommandHandler(
    ICurrentUserService currentUser,
    IUnidadPosicionRepository posiciones,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<CerrarSesionMovilCommand, Unit>
{
    public async Task<Unit> Handle(CerrarSesionMovilCommand request, CancellationToken cancellationToken)
    {
        var unidadId = currentUser.UnidadId ?? throw new ForbiddenException("La sesión no tiene una unidad asociada.");
        var agenteId = currentUser.AgenteId ?? throw new ForbiddenException("La sesión no tiene un agente asociado.");

        // Solo cierra la suya: si otro agente ya tomó la unidad, o el mismo agente inició otra
        // sesión después de que esta se cerrara, no se le corta
        var posicion = await posiciones.GetByUnidadIdAsync(unidadId, cancellationToken);
        if (posicion is not null && posicion.AgenteId == agenteId && posicion.SesionActiva
            && !(currentUser.SesionEmitidaUtc is { } emitida && posicion.SesionCerradaParaToken(emitida)))
        {
            posicion.CerrarSesion(timeProvider.GetUtcNow().UtcDateTime);
            await uow.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}

/// <summary>
/// Front desk libera la unidad (p. ej. el teléfono del agente se dañó y otro agente necesita
/// iniciar sesión con ella antes de que pase a desconectada).
/// </summary>
public record LiberarSesionUnidadCommand(int UnidadId) : IRequest<Unit>;

public class LiberarSesionUnidadCommandHandler(IUnidadPosicionRepository posiciones, IUnitOfWork uow, TimeProvider timeProvider)
    : IRequestHandler<LiberarSesionUnidadCommand, Unit>
{
    public async Task<Unit> Handle(LiberarSesionUnidadCommand request, CancellationToken cancellationToken)
    {
        var posicion = await posiciones.GetByUnidadIdAsync(request.UnidadId, cancellationToken)
            ?? throw new NotFoundException($"La unidad '{request.UnidadId}' no tiene una sesión registrada.");

        // Aunque ya figure cerrada se registra el cierre: invalida también los tokens emitidos hasta ahora
        posicion.CerrarSesion(timeProvider.GetUtcNow().UtcDateTime);
        await uow.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
