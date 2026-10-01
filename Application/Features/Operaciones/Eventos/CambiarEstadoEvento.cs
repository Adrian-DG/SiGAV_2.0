using Application.Common;
using Application.Contracts;
using Application.Exceptions;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Eventos;

// Command: la unidad llegó al lugar (Pendiente → En curso). Sin fecha se usa la hora actual.
public record IniciarAtencionEventoCommand(int EventoId, DateTime? FechaHoraLlegadaUtc = null) : IRequest;

public class IniciarAtencionEventoCommandHandler(
    IEventoRepository eventos,
    ICurrentUserService currentUser,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<IniciarAtencionEventoCommand, Unit>
{
    public async Task<Unit> Handle(IniciarAtencionEventoCommand request, CancellationToken cancellationToken)
    {
        var evento = await eventos.GetByIdAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);
        EventoAcceso.AsegurarPuedeOperar(evento, currentUser);

        var ahoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        evento.IniciarAtencion(request.FechaHoraLlegadaUtc ?? ahoraUtc, ahoraUtc);

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// Command: cierre del evento. Sin fecha se usa la hora actual.
public record CompletarEventoCommand(int EventoId, int TipoCierreId, DateTime? FechaHoraCompletadoUtc = null) : IRequest;

public class CompletarEventoCommandValidator : AbstractValidator<CompletarEventoCommand>
{
    public CompletarEventoCommandValidator(IReadDbContext db)
    {
        RuleFor(x => x.TipoCierreId)
            .GreaterThan(0).WithMessage("Indique el tipo de cierre.")
            .MustAsync((id, ct) => db.TiposCierre.ExisteActivoAsync(id, ct)).WithMessage("El tipo de cierre no es válido.");
    }
}

public class CompletarEventoCommandHandler(
    IEventoRepository eventos,
    ICurrentUserService currentUser,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<CompletarEventoCommand, Unit>
{
    public async Task<Unit> Handle(CompletarEventoCommand request, CancellationToken cancellationToken)
    {
        var evento = await eventos.GetByIdAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);
        EventoAcceso.AsegurarPuedeOperar(evento, currentUser);

        var ahoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        evento.Completar(request.FechaHoraCompletadoUtc ?? ahoraUtc, request.TipoCierreId, ahoraUtc);

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// Command: anulación lógica (deja de contar en listados y estadísticas). Solo front desk.
public record AnularEventoCommand(int EventoId) : IRequest;

public class AnularEventoCommandHandler(
    IEventoRepository eventos,
    ICurrentUserService currentUser,
    IUnitOfWork uow) : IRequestHandler<AnularEventoCommand, Unit>
{
    public async Task<Unit> Handle(AnularEventoCommand request, CancellationToken cancellationToken)
    {
        if (!EventoAcceso.EsWeb(currentUser))
            throw new ForbiddenException("Solo front desk puede anular eventos.");

        var evento = await eventos.GetByIdAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);

        evento.Anular();

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
