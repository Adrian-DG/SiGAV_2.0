using Application.Common;
using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Denominaciones;

// Command
public record CreateDenominacionCommand(string Nombre, int TramoId, int NivelDenominacionId) : IRequest<int>;

// Validator
public class CreateDenominacionCommandValidator : AbstractValidator<CreateDenominacionCommand>
{
    public CreateDenominacionCommandValidator(IReadDbContext db)
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(Denominacion.NombreMaxLength).WithMessage($"El nombre no puede exceder {Denominacion.NombreMaxLength} caracteres.");
        RuleFor(x => x.TramoId)
            .GreaterThan(0).WithMessage("El tramo es requerido.")
            .MustAsync((id, ct) => db.Tramos.ExisteActivoAsync(id, ct)).WithMessage("El tramo especificado no existe.");
        RuleFor(x => x.NivelDenominacionId)
            .GreaterThan(0).WithMessage("El nivel de la denominación es requerido.")
            .MustAsync((id, ct) => db.NivelesDenominacion.ExisteActivoAsync(id, ct)).WithMessage("El nivel de denominación especificado no existe.");
    }
}

// Handler
public class CreateDenominacionCommandHandler(IDenominacionRepository denominaciones, IUnitOfWork uow)
    : IRequestHandler<CreateDenominacionCommand, int>
{
    public async Task<int> Handle(CreateDenominacionCommand request, CancellationToken cancellationToken)
    {
        var denominacion = Denominacion.Crear(request.Nombre, request.TramoId, request.NivelDenominacionId);

        if (await denominaciones.ExisteNombreAsync(denominacion.Nombre, cancellationToken))
            throw new ConflictException($"La denominación '{denominacion.Nombre}' ya existe.");

        denominaciones.Add(denominacion);
        await uow.SaveChangesAsync(cancellationToken);

        return denominacion.Id;
    }
}
