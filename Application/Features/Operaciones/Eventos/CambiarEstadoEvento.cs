using Application.Common;
using Application.Contracts;
using Application.Contracts.Notificaciones;
using Application.Exceptions;
using Domain.Enums;
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
    INotificadorUnidades notificador,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<AnularEventoCommand, Unit>
{
    public async Task<Unit> Handle(AnularEventoCommand request, CancellationToken cancellationToken)
    {
        if (!EventoAcceso.EsWeb(currentUser))
            throw new ForbiddenException("Solo front desk puede anular eventos.");

        var evento = await eventos.GetByIdAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);

        // Solo los que la unidad aún tiene abiertos en la app (los asignados sin completar)
        var avisar = evento.IsActive && evento.Estado != EstadoEventoEnum.Completado;
        evento.Anular();

        await uow.SaveChangesAsync(cancellationToken);

        if (avisar)
        {
            try
            {
                await notificador.NotificarUnidadesAsync(
                    evento.Unidades.Select(u => u.UnidadId).ToList(),
                    new NotificacionUnidad(
                        TiposNotificacion.EventoAnulado,
                        $"Evento No. {evento.Id} anulado",
                        "El centro de operaciones anuló el evento: ya no debe atenderlo.",
                        timeProvider.GetUtcNow().UtcDateTime,
                        evento.Id),
                    cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // La anulación ya quedó guardada; la app deja de verlo al refrescar sus eventos asignados
            }
        }

        return Unit.Value;
    }
}

// Command: cambia el agente que atiende el evento por parte de una unidad (p. ej. el componente se
// queda en el lugar mientras el otro agente va a otro evento). Desde la app la unidad es la de la
// sesión y el agente debe ser el de la sesión o su componente; front desk indica la unidad.
public record CambiarAgenteEventoCommand(int EventoId, int AgenteId, int? UnidadId = null) : IRequest;

public class CambiarAgenteEventoCommandValidator : AbstractValidator<CambiarAgenteEventoCommand>
{
    public CambiarAgenteEventoCommandValidator()
    {
        RuleFor(x => x.AgenteId).GreaterThan(0).WithMessage("Indique el agente que atiende el evento.");
        RuleFor(x => x.UnidadId).GreaterThan(0).When(x => x.UnidadId.HasValue).WithMessage("La unidad no es válida.");
    }
}

public class CambiarAgenteEventoCommandHandler(
    IEventoRepository eventos,
    IAgenteRepository agentes,
    ICurrentUserService currentUser,
    IUnitOfWork uow) : IRequestHandler<CambiarAgenteEventoCommand, Unit>
{
    public async Task<Unit> Handle(CambiarAgenteEventoCommand request, CancellationToken cancellationToken)
    {
        var evento = await eventos.GetByIdAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);
        EventoAcceso.AsegurarParticipa(evento, currentUser);

        int unidadId;
        if (EventoAcceso.EsWeb(currentUser))
        {
            unidadId = request.UnidadId ?? evento.UnidadPrincipal.UnidadId;
            if (await agentes.GetByIdAsync(request.AgenteId, cancellationToken) is not { IsActive: true })
                throw new NotFoundException("El agente", request.AgenteId);
        }
        else
        {
            unidadId = EventoAcceso.UnidadDeAlcance(currentUser)!.Value;
            var agenteId = EventoAcceso.AgenteDeLaSesion(currentUser, request.AgenteId);
            await EventoAcceso.AsegurarAgenteVigenteAsync(agentes, currentUser, agenteId, cancellationToken);
        }

        evento.CambiarAgenteUnidad(unidadId, request.AgenteId);

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
