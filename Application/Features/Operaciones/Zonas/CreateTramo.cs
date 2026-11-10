using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Zonas;

// Command: crea un tramo en una zona activa; queda disponible para asignarlo a las denominaciones
public record CreateTramoCommand(string Nombre, int ZonaId) : IRequest<int>;

// Validator
public class CreateTramoCommandValidator : AbstractValidator<CreateTramoCommand>
{
    public CreateTramoCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(Tramo.NombreMaxLength).WithMessage($"El nombre no puede exceder {Tramo.NombreMaxLength} caracteres.");
        RuleFor(x => x.ZonaId).GreaterThan(0).WithMessage("Seleccione la zona del tramo.");
    }
}

// Handler
public class CreateTramoCommandHandler(IZonaRepository zonas, IUnitOfWork uow) : IRequestHandler<CreateTramoCommand, int>
{
    public async Task<int> Handle(CreateTramoCommand request, CancellationToken cancellationToken)
    {
        var zona = await zonas.GetZonaAsync(request.ZonaId, cancellationToken)
            ?? throw new NotFoundException("La zona", request.ZonaId);

        var tramo = Tramo.Crear(request.Nombre, zona);

        if (await zonas.ExisteNombreTramoAsync(tramo.Nombre, cancellationToken))
            throw new ConflictException($"El tramo '{tramo.Nombre}' ya existe.");

        zonas.Add(tramo);
        await uow.SaveChangesAsync(cancellationToken);
        return tramo.Id;
    }
}
