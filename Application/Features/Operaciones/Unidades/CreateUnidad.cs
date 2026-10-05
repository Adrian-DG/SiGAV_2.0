using Application.Common;
using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

// Command: crea una unidad. Sin DenominacionId queda sin denominación (No disponible) hasta que se
// le asigne una; con DenominacionId se le asigna, liberándola de la unidad que la tuviera.
public record CreateUnidadCommand(string Ficha, string? Placa, int? DenominacionId = null, string? Motivo = null) : IRequest<int>;

// Validator
public class CreateUnidadCommandValidator : AbstractValidator<CreateUnidadCommand>
{
    public CreateUnidadCommandValidator()
    {
        RuleFor(x => new NuevaUnidadRequest(x.Ficha, x.Placa))
            .SetValidator(new NuevaUnidadValidator())
            .OverridePropertyName(string.Empty);
        RuleFor(x => x.DenominacionId).GreaterThan(0).When(x => x.DenominacionId.HasValue).WithMessage("La denominación no es válida.");
        RuleFor(x => x.Motivo)
            .MaximumLength(AutorCambio.ObservacionMaxLength).WithMessage($"El motivo no puede exceder {AutorCambio.ObservacionMaxLength} caracteres.");
    }
}

// Handler
public class CreateUnidadCommandHandler(
    IUnidadRepository unidades,
    IDenominacionRepository denominaciones,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IRequestHandler<CreateUnidadCommand, int>
{
    public async Task<int> Handle(CreateUnidadCommand request, CancellationToken cancellationToken)
    {
        var autor = currentUser.RequerirAutorWeb(timeProvider, request.Motivo);

        var unidad = Unidad.Crear(FichaUnidad.Normalizar(request.Ficha), request.Placa, autor);

        if (await unidades.ExisteFichaAsync(unidad.Ficha, cancellationToken: cancellationToken))
            throw new ConflictException($"La ficha '{unidad.Ficha}' ya está registrada en el sistema.");

        if (request.DenominacionId is { } denominacionId)
        {
            var denominacion = await denominaciones.GetByIdAsync(denominacionId, cancellationToken)
                ?? throw new NotFoundException("La denominación", denominacionId);

            var ocupantes = await unidades.GetActivasConDenominacionAsync(denominacion.Id, cancellationToken);
            AsignacionDenominacionService.Asignar(unidad, denominacion, ocupantes, autor);
        }

        unidades.Add(unidad);
        await uow.SaveChangesAsync(cancellationToken);

        return unidad.Id;
    }
}
