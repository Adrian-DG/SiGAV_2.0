using Application.Contracts.Operaciones;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Denominaciones;

// Command
public record CreateDenominacionCommand(string Nombre, int TramoId, int TipoUnidadId) : IRequest<int>;

// Validator
public class CreateDenominacionCommandValidator : AbstractValidator<CreateDenominacionCommand>
{
    public CreateDenominacionCommandValidator(ICatalogoQueries catalogos)
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(Denominacion.NombreMaxLength).WithMessage($"El nombre no puede exceder {Denominacion.NombreMaxLength} caracteres.");
        RuleFor(x => x.TramoId)
            .GreaterThan(0).WithMessage("El tramo es requerido.")
            .MustAsync(catalogos.ExisteTramoAsync).WithMessage("El tramo especificado no existe.");
        RuleFor(x => x.TipoUnidadId)
            .GreaterThan(0).WithMessage("El tipo de unidad es requerido.")
            .MustAsync(catalogos.ExisteTipoUnidadAsync).WithMessage("El tipo de unidad especificado no existe.");
    }
}

// Handler
public class CreateDenominacionCommandHandler(IDenominacionRepository denominaciones, IUnitOfWork uow)
    : IRequestHandler<CreateDenominacionCommand, int>
{
    public async Task<int> Handle(CreateDenominacionCommand request, CancellationToken cancellationToken)
    {
        var denominacion = Denominacion.Crear(request.Nombre, request.TramoId, request.TipoUnidadId);

        if (await denominaciones.ExisteNombreAsync(denominacion.Nombre, cancellationToken))
            throw new ConflictException($"La denominación '{denominacion.Nombre}' ya existe.");

        denominaciones.Add(denominacion);
        await uow.SaveChangesAsync(cancellationToken);

        return denominacion.Id;
    }
}
