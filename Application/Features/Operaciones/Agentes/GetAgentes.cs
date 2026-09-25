using System.Linq.Expressions;
using Application.Common;
using Application.Common.Models;
using Application.Contracts;
using Application.Exceptions;
using Domain.Enums;
using FluentValidation;
using Domain.Entities.Operaciones;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Agentes;

internal static class AgenteProyecciones
{
    // La Armada (ARD) usa su propia nomenclatura de rangos (misma regla que Agente.GetRango)
    public static readonly Expression<Func<Agente, AgenteViewModel>> ToViewModel = a => new AgenteViewModel(
        a.Id,
        a.Identificacion,
        a.Nombre,
        a.Apellido,
        a.Apellido + " " + a.Nombre,
        a.Sexo,
        a.Institucion,
        a.RangoId,
        a.Institucion == InstitucionEnum.ARD ? a.Rango!.NombreArmada : a.Rango!.Nombre,
        a.AreaOperativa,
        a.AccesoTotal,
        a.Especialidad,
        a.Autorizado,
        a.IsActive,
        a.CreatedAt);
}

// Query: listado paginado. Autorizado = false sirve para la bandeja de registros pendientes.
public record GetAgentesQuery(
    int Page = 1,
    int Size = 10,
    string? SearchTerm = null,
    bool? Autorizado = null,
    AreaOperativaEnum? AreaOperativa = null,
    bool IncluirInactivos = false) : IRequest<PagedResult<AgenteViewModel>>, IPagedQuery;

public class GetAgentesQueryValidator : PagedQueryValidator<GetAgentesQuery>
{
    public GetAgentesQueryValidator()
    {
        RuleFor(x => x.AreaOperativa).IsInEnum().When(x => x.AreaOperativa.HasValue).WithMessage("El área operativa no es válida.");
    }
}

public class GetAgentesQueryHandler(IReadDbContext db) : IRequestHandler<GetAgentesQuery, PagedResult<AgenteViewModel>>
{
    public Task<PagedResult<AgenteViewModel>> Handle(GetAgentesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Agentes;

        if (!request.IncluirInactivos) query = query.Where(a => a.IsActive);
        if (request.Autorizado is { } autorizado) query = query.Where(a => a.Autorizado == autorizado);
        if (request.AreaOperativa is { } area) query = query.Where(a => a.AreaOperativa == area);

        if (QueryableExtensions.PatronBusqueda(request.SearchTerm) is { } patron)
            query = query.Where(a => EF.Functions.Like(a.Identificacion, patron)
                || EF.Functions.Like(a.Nombre, patron)
                || EF.Functions.Like(a.Apellido, patron));

        return query
            .OrderBy(a => a.Apellido)
            .ThenBy(a => a.Nombre)
            .Select(AgenteProyecciones.ToViewModel)
            .ToPagedResultAsync(request.Page, request.Size, cancellationToken);
    }
}

// Query: detalle de un agente
public record GetAgenteQuery(int AgenteId) : IRequest<AgenteViewModel>;

public class GetAgenteQueryHandler(IReadDbContext db) : IRequestHandler<GetAgenteQuery, AgenteViewModel>
{
    public async Task<AgenteViewModel> Handle(GetAgenteQuery request, CancellationToken cancellationToken)
        => await db.Agentes
            .Where(a => a.Id == request.AgenteId)
            .Select(AgenteProyecciones.ToViewModel)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("El agente", request.AgenteId);
}

// Query: sugerencias de agentes activos y autorizados (p. ej. personal de pre-hospitalaria)
public record GetAgentesAutoCompleteQuery(string? Term, AreaOperativaEnum? AreaOperativa) : IRequest<IReadOnlyList<AgenteAutoCompleteViewModel>>;

public class GetAgentesAutoCompleteQueryHandler(IReadDbContext db)
    : IRequestHandler<GetAgentesAutoCompleteQuery, IReadOnlyList<AgenteAutoCompleteViewModel>>
{
    private const int MaxResultados = 10;

    public async Task<IReadOnlyList<AgenteAutoCompleteViewModel>> Handle(GetAgentesAutoCompleteQuery request, CancellationToken cancellationToken)
    {
        var patron = QueryableExtensions.PatronBusqueda(request.Term) ?? "%";

        var query = db.Agentes
            .Where(a => a.IsActive && a.Autorizado)
            .Where(a => EF.Functions.Like(a.Identificacion, patron)
                || EF.Functions.Like(a.Nombre, patron)
                || EF.Functions.Like(a.Apellido, patron));

        if (request.AreaOperativa is { } area) query = query.Where(a => a.AreaOperativa == area);

        return await query
            .OrderBy(a => a.Apellido)
            .ThenBy(a => a.Nombre)
            .Take(MaxResultados)
            .Select(a => new AgenteAutoCompleteViewModel(
                a.Id,
                a.Identificacion,
                (a.Institucion == InstitucionEnum.ARD ? a.Rango!.NombreArmada : a.Rango!.Nombre) + ", " + a.Apellido + " " + a.Nombre,
                a.AreaOperativa))
            .ToListAsync(cancellationToken);
    }
}

// Query: validación previa al login de la app (anónima): ¿existe la cédula y está autorizada?
public record ConfirmAgenteQuery(string Cedula) : IRequest<ConfirmAgenteViewModel>;

public class ConfirmAgenteQueryValidator : AbstractValidator<ConfirmAgenteQuery>
{
    public ConfirmAgenteQueryValidator()
    {
        RuleFor(x => x.Cedula).NotEmpty().WithMessage("La cédula es requerida.");
    }
}

public class ConfirmAgenteQueryHandler(IReadDbContext db) : IRequestHandler<ConfirmAgenteQuery, ConfirmAgenteViewModel>
{
    public async Task<ConfirmAgenteViewModel> Handle(ConfirmAgenteQuery request, CancellationToken cancellationToken)
    {
        var cedula = new string(request.Cedula.Where(char.IsLetterOrDigit).ToArray());

        var estado = await db.Agentes
            .Where(a => a.Identificacion == cedula)
            .Select(a => new { a.IsActive, a.Autorizado })
            .FirstOrDefaultAsync(cancellationToken);

        return new ConfirmAgenteViewModel(estado is not null, estado is { IsActive: true, Autorizado: true });
    }
}
