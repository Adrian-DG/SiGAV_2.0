using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Zonas;

// Command: crea una zona dentro de una región
public record CreateZonaCommand(string Nombre, RegionMacroEnum Region) : IRequest<int>;

// Validator
public class CreateZonaCommandValidator : AbstractValidator<CreateZonaCommand>
{
    public CreateZonaCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(Zona.NombreMaxLength).WithMessage($"El nombre no puede exceder {Zona.NombreMaxLength} caracteres.");
        RuleFor(x => x.Region).IsInEnum().WithMessage("Seleccione la región de la zona.");
    }
}

// Handler
public class CreateZonaCommandHandler(IZonaRepository zonas, IUnitOfWork uow) : IRequestHandler<CreateZonaCommand, int>
{
    public async Task<int> Handle(CreateZonaCommand request, CancellationToken cancellationToken)
    {
        var zona = Zona.Crear(request.Nombre, request.Region);

        if (await zonas.ExisteNombreZonaAsync(zona.Nombre, cancellationToken))
            throw new ConflictException($"La zona '{zona.Nombre}' ya existe.");

        zonas.Add(zona);
        await uow.SaveChangesAsync(cancellationToken);
        return zona.Id;
    }
}
