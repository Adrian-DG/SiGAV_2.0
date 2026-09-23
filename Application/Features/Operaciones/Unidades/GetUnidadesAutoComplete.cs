using Application.Contracts.Operaciones;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

// Query: sugerencias de unidades activas por ficha o denominación
public record GetUnidadesAutoCompleteQuery(string? Term, bool EsAmbulancia) : IRequest<IReadOnlyList<UnidadAutoCompleteViewModel>>;

// Handler
public class GetUnidadesAutoCompleteQueryHandler(IUnidadQueries queries)
    : IRequestHandler<GetUnidadesAutoCompleteQuery, IReadOnlyList<UnidadAutoCompleteViewModel>>
{
    private const int MaxResultados = 10;

    public Task<IReadOnlyList<UnidadAutoCompleteViewModel>> Handle(GetUnidadesAutoCompleteQuery request, CancellationToken cancellationToken)
        => queries.AutoCompleteAsync(request.Term?.Trim() ?? string.Empty, request.EsAmbulancia, MaxResultados, cancellationToken);
}
