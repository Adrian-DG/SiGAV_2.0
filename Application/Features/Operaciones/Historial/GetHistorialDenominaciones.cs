using Application.Common;
using Application.Common.Models;
using Application.Contracts;
using Domain.Entities.Operaciones;
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

public class GetHistorialDenominacionesUnidadQueryHandler(IReadDbContext db)
    : IRequestHandler<GetHistorialDenominacionesUnidadQuery, PagedResult<HistorialDenominacionViewModel>>
{
    public Task<PagedResult<HistorialDenominacionViewModel>> Handle(GetHistorialDenominacionesUnidadQuery request, CancellationToken cancellationToken)
        => HistorialProyeccion
            .Proyectar(db, db.HistorialDenominaciones.Where(h => h.UnidadId == request.UnidadId))
            .ToPagedResultAsync(request.Page, request.Size, cancellationToken);
}

// Query: qué unidades han tenido una denominación y cuándo la perdieron
public record GetHistorialUnidadesDenominacionQuery(int DenominacionId, int Page = 1, int Size = 20)
    : IRequest<PagedResult<HistorialDenominacionViewModel>>, IPagedQuery;

public class GetHistorialUnidadesDenominacionQueryValidator : PagedQueryValidator<GetHistorialUnidadesDenominacionQuery>;

public class GetHistorialUnidadesDenominacionQueryHandler(IReadDbContext db)
    : IRequestHandler<GetHistorialUnidadesDenominacionQuery, PagedResult<HistorialDenominacionViewModel>>
{
    public Task<PagedResult<HistorialDenominacionViewModel>> Handle(GetHistorialUnidadesDenominacionQuery request, CancellationToken cancellationToken)
        => HistorialProyeccion
            .Proyectar(db, db.HistorialDenominaciones.Where(h =>
                h.DenominacionAnteriorId == request.DenominacionId || h.DenominacionNuevaId == request.DenominacionId))
            .ToPagedResultAsync(request.Page, request.Size, cancellationToken);
}

internal static class HistorialProyeccion
{
    /// <summary>Más recientes primero, con la ficha de la unidad y el usuario responsable.</summary>
    public static IQueryable<HistorialDenominacionViewModel> Proyectar(IReadDbContext db, IQueryable<HistorialDenominacionUnidad> historial)
        => from h in historial
           join u in db.Unidades on h.UnidadId equals u.Id
           join usr in db.Usuarios on h.UsuarioId equals usr.Id into usuarios
           from usr in usuarios.DefaultIfEmpty()
           orderby h.FechaUtc descending, h.Id descending
           select new HistorialDenominacionViewModel(
               h.Id,
               h.FechaUtc,
               h.TipoCambio,
               h.UnidadId,
               u.Ficha,
               h.DenominacionAnteriorId,
               h.DenominacionAnterior != null ? h.DenominacionAnterior.Nombre : null,
               h.DenominacionNuevaId,
               h.DenominacionNueva != null ? h.DenominacionNueva.Nombre : null,
               h.UnidadRelacionadaId,
               h.UnidadRelacionada != null ? h.UnidadRelacionada.Ficha : null,
               h.UsuarioId,
               usr != null ? usr.UserName : null,
               h.Observacion);
}
