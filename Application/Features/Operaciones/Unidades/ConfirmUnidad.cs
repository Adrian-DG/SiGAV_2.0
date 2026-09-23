using Application.Contracts.Operaciones;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

// Query: validación previa al login de la app (la unidad existe, está activa y disponible)
public record ConfirmUnidadExisteQuery(string Ficha) : IRequest<bool>;

public class ConfirmUnidadExisteQueryValidator : AbstractValidator<ConfirmUnidadExisteQuery>
{
    public ConfirmUnidadExisteQueryValidator()
    {
        RuleFor(x => x.Ficha).NotEmpty().WithMessage("La ficha es requerida.");
    }
}

public class ConfirmUnidadExisteQueryHandler(IUnidadQueries queries) : IRequestHandler<ConfirmUnidadExisteQuery, bool>
{
    public Task<bool> Handle(ConfirmUnidadExisteQuery request, CancellationToken cancellationToken)
        => queries.ExisteActivaYDisponibleAsync(request.Ficha.Trim(), cancellationToken);
}

// Query: disponibilidad actual de la unidad
public record ConfirmUnidadDisponibleQuery(string Ficha) : IRequest<bool>;

public class ConfirmUnidadDisponibleQueryValidator : AbstractValidator<ConfirmUnidadDisponibleQuery>
{
    public ConfirmUnidadDisponibleQueryValidator()
    {
        RuleFor(x => x.Ficha).NotEmpty().WithMessage("La ficha es requerida.");
    }
}

public class ConfirmUnidadDisponibleQueryHandler(IUnidadQueries queries) : IRequestHandler<ConfirmUnidadDisponibleQuery, bool>
{
    public Task<bool> Handle(ConfirmUnidadDisponibleQuery request, CancellationToken cancellationToken)
        => queries.EstaDisponibleAsync(request.Ficha.Trim(), cancellationToken);
}
