using Application.Contracts.Operaciones;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

// Query
public record GetUnidadesPorTramoQuery(int TramoId) : IRequest<IReadOnlyList<UnidadViewModel>>;

// Validator
public class GetUnidadesPorTramoQueryValidator : AbstractValidator<GetUnidadesPorTramoQuery>
{
    public GetUnidadesPorTramoQueryValidator()
    {
        RuleFor(x => x.TramoId).GreaterThan(0).WithMessage("El tramo es requerido.");
    }
}

// Handler
public class GetUnidadesPorTramoQueryHandler(IUnidadQueries queries)
    : IRequestHandler<GetUnidadesPorTramoQuery, IReadOnlyList<UnidadViewModel>>
{
    public Task<IReadOnlyList<UnidadViewModel>> Handle(GetUnidadesPorTramoQuery request, CancellationToken cancellationToken)
        => queries.GetByTramoAsync(request.TramoId, cancellationToken);
}
