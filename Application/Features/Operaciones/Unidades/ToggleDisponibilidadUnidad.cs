using Application.Exceptions;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

// Command: alterna la disponibilidad operativa de la unidad (usado desde la app móvil)
public record ToggleDisponibilidadUnidadCommand(string Ficha) : IRequest<bool>;

// Validator
public class ToggleDisponibilidadUnidadCommandValidator : AbstractValidator<ToggleDisponibilidadUnidadCommand>
{
    public ToggleDisponibilidadUnidadCommandValidator()
    {
        RuleFor(x => x.Ficha).NotEmpty().WithMessage("La ficha es requerida.");
    }
}

// Handler: devuelve la nueva disponibilidad
public class ToggleDisponibilidadUnidadCommandHandler(IUnidadRepository unidades, IUnitOfWork uow)
    : IRequestHandler<ToggleDisponibilidadUnidadCommand, bool>
{
    public async Task<bool> Handle(ToggleDisponibilidadUnidadCommand request, CancellationToken cancellationToken)
    {
        var unidad = await unidades.GetByFichaAsync(request.Ficha.Trim(), cancellationToken)
            ?? throw new NotFoundException("La unidad con ficha", request.Ficha);

        unidad.AlternarDisponibilidad();
        await uow.SaveChangesAsync(cancellationToken);

        return unidad.EstaDisponible;
    }
}
