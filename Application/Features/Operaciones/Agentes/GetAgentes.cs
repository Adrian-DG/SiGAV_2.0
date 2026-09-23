using Application.Common.Models;
using Application.Contracts.Operaciones;
using Application.Exceptions;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Agentes;

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

public class GetAgentesQueryHandler(IAgenteQueries queries) : IRequestHandler<GetAgentesQuery, PagedResult<AgenteViewModel>>
{
    public Task<PagedResult<AgenteViewModel>> Handle(GetAgentesQuery request, CancellationToken cancellationToken)
        => queries.GetPagedAsync(request.Page, request.Size, request.SearchTerm, request.Autorizado,
            request.AreaOperativa, request.IncluirInactivos, cancellationToken);
}

// Query: detalle de un agente
public record GetAgenteQuery(int AgenteId) : IRequest<AgenteViewModel>;

public class GetAgenteQueryHandler(IAgenteQueries queries) : IRequestHandler<GetAgenteQuery, AgenteViewModel>
{
    public async Task<AgenteViewModel> Handle(GetAgenteQuery request, CancellationToken cancellationToken)
        => await queries.GetByIdAsync(request.AgenteId, cancellationToken)
            ?? throw new NotFoundException("El agente", request.AgenteId);
}

// Query: sugerencias de agentes activos y autorizados (p. ej. personal de pre-hospitalaria)
public record GetAgentesAutoCompleteQuery(string? Term, AreaOperativaEnum? AreaOperativa) : IRequest<IReadOnlyList<AgenteAutoCompleteViewModel>>;

public class GetAgentesAutoCompleteQueryHandler(IAgenteQueries queries)
    : IRequestHandler<GetAgentesAutoCompleteQuery, IReadOnlyList<AgenteAutoCompleteViewModel>>
{
    private const int MaxResultados = 10;

    public Task<IReadOnlyList<AgenteAutoCompleteViewModel>> Handle(GetAgentesAutoCompleteQuery request, CancellationToken cancellationToken)
        => queries.AutoCompleteAsync(request.Term?.Trim() ?? string.Empty, request.AreaOperativa, MaxResultados, cancellationToken);
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

public class ConfirmAgenteQueryHandler(IAgenteQueries queries) : IRequestHandler<ConfirmAgenteQuery, ConfirmAgenteViewModel>
{
    public Task<ConfirmAgenteViewModel> Handle(ConfirmAgenteQuery request, CancellationToken cancellationToken)
    {
        var cedula = new string(request.Cedula.Where(char.IsLetterOrDigit).ToArray());
        return queries.ConfirmarAsync(cedula, cancellationToken);
    }
}
