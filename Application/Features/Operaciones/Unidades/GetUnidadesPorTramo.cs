using Application.Contracts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
public class GetUnidadesPorTramoQueryHandler(IReadDbContext db)
    : IRequestHandler<GetUnidadesPorTramoQuery, IReadOnlyList<UnidadViewModel>>
{
    public async Task<IReadOnlyList<UnidadViewModel>> Handle(GetUnidadesPorTramoQuery request, CancellationToken cancellationToken)
        => await db.Unidades
            .Where(u => u.IsActive && u.Denominacion != null && u.Denominacion.TramoId == request.TramoId)
            .OrderBy(u => u.Ficha)
            .Select(UnidadViewModel.Proyeccion)
            .ToListAsync(cancellationToken);
}
