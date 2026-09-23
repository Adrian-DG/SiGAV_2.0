using Application.Common.Models;
using Application.Contracts.Operaciones;
using Domain.Enums;
using MediatR;

namespace Application.Features.Operaciones.Historial;

public record HistorialDenominacionViewModel(
    long Id,
    DateTime FechaUtc,
    TipoCambioDenominacionEnum TipoCambio,
    int UnidadId,
    string Ficha,
    int? DenominacionAnteriorId,
    string? DenominacionAnterior,
    int? DenominacionNuevaId,
    string? DenominacionNueva,
    int? UnidadRelacionadaId,
    string? FichaRelacionada,
    int UsuarioId,
    string? Usuario,
    string? Observacion);

// Query: cambios de denominación de una unidad (más recientes primero)
public record GetHistorialDenominacionesUnidadQuery(int UnidadId, int Page = 1, int Size = 20)
    : IRequest<PagedResult<HistorialDenominacionViewModel>>, IPagedQuery;

public class GetHistorialDenominacionesUnidadQueryValidator : PagedQueryValidator<GetHistorialDenominacionesUnidadQuery>;

public class GetHistorialDenominacionesUnidadQueryHandler(IHistorialDenominacionQueries queries)
    : IRequestHandler<GetHistorialDenominacionesUnidadQuery, PagedResult<HistorialDenominacionViewModel>>
{
    public Task<PagedResult<HistorialDenominacionViewModel>> Handle(GetHistorialDenominacionesUnidadQuery request, CancellationToken cancellationToken)
        => queries.GetByUnidadAsync(request.UnidadId, request.Page, request.Size, cancellationToken);
}

// Query: qué unidades han tenido una denominación y cuándo la perdieron
public record GetHistorialUnidadesDenominacionQuery(int DenominacionId, int Page = 1, int Size = 20)
    : IRequest<PagedResult<HistorialDenominacionViewModel>>, IPagedQuery;

public class GetHistorialUnidadesDenominacionQueryValidator : PagedQueryValidator<GetHistorialUnidadesDenominacionQuery>;

public class GetHistorialUnidadesDenominacionQueryHandler(IHistorialDenominacionQueries queries)
    : IRequestHandler<GetHistorialUnidadesDenominacionQuery, PagedResult<HistorialDenominacionViewModel>>
{
    public Task<PagedResult<HistorialDenominacionViewModel>> Handle(GetHistorialUnidadesDenominacionQuery request, CancellationToken cancellationToken)
        => queries.GetByDenominacionAsync(request.DenominacionId, request.Page, request.Size, cancellationToken);
}
