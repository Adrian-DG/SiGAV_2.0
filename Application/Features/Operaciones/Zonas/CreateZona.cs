using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Zonas;

// Command: crea una zona dentro de una región, opcionalmente con sus tramos (todo o nada)
public record CreateZonaCommand(string Nombre, RegionMacroEnum Region, IReadOnlyList<string>? Tramos = null) : IRequest<int>;

// Validator
public class CreateZonaCommandValidator : AbstractValidator<CreateZonaCommand>
{
    public CreateZonaCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(Zona.NombreMaxLength).WithMessage($"El nombre no puede exceder {Zona.NombreMaxLength} caracteres.");
        RuleFor(x => x.Region).IsInEnum().WithMessage("Seleccione la región de la zona.");

        RuleForEach(x => x.Tramos)
            .NotEmpty().WithMessage("El nombre del tramo es requerido.")
            .MaximumLength(Tramo.NombreMaxLength).WithMessage($"El nombre del tramo no puede exceder {Tramo.NombreMaxLength} caracteres.");
        RuleFor(x => x.Tramos)
            .Must(t => t!.Select(n => n.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase).Count() == t!.Count)
            .When(x => x.Tramos is { Count: > 0 } && x.Tramos.All(n => !string.IsNullOrWhiteSpace(n)))
            .WithMessage("Hay tramos repetidos.");
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

        var tramos = (request.Tramos ?? []).Select(zona.NuevoTramo).ToList();
        foreach (var tramo in tramos)
        {
            if (await zonas.ExisteNombreTramoAsync(tramo.Nombre, cancellationToken))
                throw new ConflictException($"El tramo '{tramo.Nombre}' ya existe.");
        }

        zonas.Add(zona);
        foreach (var tramo in tramos) zonas.Add(tramo);

        // Un solo SaveChanges: la zona y sus tramos se guardan juntos o no se guarda nada
        await uow.SaveChangesAsync(cancellationToken);
        return zona.Id;
    }
}
