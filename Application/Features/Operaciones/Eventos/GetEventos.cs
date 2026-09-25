using Application.Common;
using Application.Common.Models;
using Application.Contracts;
using Application.Contracts.Operaciones;
using Application.Exceptions;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Eventos;

// Query: listado paginado, más recientes primero. Las fechas son días operativos de RD.
// En la app solo aparecen los eventos en que participa su unidad y nunca los anulados.
public record GetEventosQuery(
    EstadoEventoEnum? Estado = null,
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    bool IncluirAnulados = false,
    int Page = 1,
    int Size = 20) : IRequest<PagedResult<EventoListItemViewModel>>, IPagedQuery;

public class GetEventosQueryValidator : PagedQueryValidator<GetEventosQuery>
{
    public GetEventosQueryValidator()
    {
        RuleFor(x => x.Estado).IsInEnum().When(x => x.Estado.HasValue).WithMessage("El estado no es válido.");
        RuleFor(x => x.Desde)
            .Must((q, desde) => desde is null || q.Hasta is null || desde <= q.Hasta)
            .WithMessage("La fecha inicial no puede ser posterior a la final.");
    }
}

public class GetEventosQueryHandler(IEventoQueries queries, ICurrentUserService currentUser)
    : IRequestHandler<GetEventosQuery, PagedResult<EventoListItemViewModel>>
{
    public Task<PagedResult<EventoListItemViewModel>> Handle(GetEventosQuery request, CancellationToken cancellationToken)
    {
        var unidadId = EventoAcceso.UnidadDeAlcance(currentUser);

        DateTime? desdeUtc = request.Desde is { } d ? ZonaHorariaOperativa.InicioDelDiaUtc(d) : null;
        DateTime? hastaUtc = request.Hasta is { } h ? ZonaHorariaOperativa.InicioDelDiaUtc(h.AddDays(1)) : null;

        var filtro = new FiltroEventos(
            request.Estado,
            desdeUtc,
            hastaUtc,
            unidadId,
            // Los anulados solo los ve front desk, y solo si los pide
            IncluirAnulados: unidadId is null && request.IncluirAnulados);

        return queries.GetPagedAsync(filtro, request.Page, request.Size, cancellationToken);
    }
}

// Query: detalle. Para la app, un evento de otra unidad responde 404 (no se revela que existe).
public record GetEventoQuery(int EventoId) : IRequest<EventoDetalleViewModel>;

public class GetEventoQueryHandler(IEventoQueries queries, ICurrentUserService currentUser)
    : IRequestHandler<GetEventoQuery, EventoDetalleViewModel>
{
    public async Task<EventoDetalleViewModel> Handle(GetEventoQuery request, CancellationToken cancellationToken)
        => await queries.GetDetalleAsync(request.EventoId, EventoAcceso.UnidadDeAlcance(currentUser), cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);
}
