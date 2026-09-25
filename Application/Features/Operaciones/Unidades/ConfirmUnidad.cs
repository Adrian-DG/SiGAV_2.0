using Application.Contracts;
using Domain.Entities.Operaciones;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Unidades;

// Query: validación previa al login de la app (la unidad existe, está activa y disponible)
public record ConfirmUnidadExisteQuery(string Ficha) : IRequest<bool>;

public class ConfirmUnidadExisteQueryValidator : AbstractValidator<ConfirmUnidadExisteQuery>
{
    public ConfirmUnidadExisteQueryValidator()
    {
        RuleFor(x => x.Ficha)
            .NotEmpty().WithMessage("La ficha es requerida.")
            .Matches(Unidad.FichaRegex).WithMessage($"La ficha debe cumplir con el formato valido.");
    }
}

public class ConfirmUnidadExisteQueryHandler(IReadDbContext db) : IRequestHandler<ConfirmUnidadExisteQuery, bool>
{
    public Task<bool> Handle(ConfirmUnidadExisteQuery request, CancellationToken cancellationToken)
    {
        var ficha = request.Ficha.Trim();
        return db.Unidades.AnyAsync(u => u.Ficha == ficha && u.IsActive && u.EstaDisponible, cancellationToken);
    }
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

public class ConfirmUnidadDisponibleQueryHandler(IReadDbContext db) : IRequestHandler<ConfirmUnidadDisponibleQuery, bool>
{
    public Task<bool> Handle(ConfirmUnidadDisponibleQuery request, CancellationToken cancellationToken)
    {
        var ficha = request.Ficha.Trim();
        return db.Unidades.AnyAsync(u => u.Ficha == ficha && u.EstaDisponible, cancellationToken);
    }
}
