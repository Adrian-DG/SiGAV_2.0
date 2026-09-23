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

// Command: crea una unidad y le asigna una denominación existente
public record CreateUnidadCommand(string Ficha, string? Placa, int DenominacionId, string? Motivo = null) : IRequest<int>;

// Validator
public class CreateUnidadCommandValidator : AbstractValidator<CreateUnidadCommand>
{
    public CreateUnidadCommandValidator()
    {
        RuleFor(x => x.Ficha)
            .NotEmpty().WithMessage("La ficha es requerida.")
            .MaximumLength(Unidad.FichaMaxLength).WithMessage($"La ficha no puede exceder {Unidad.FichaMaxLength} caracteres.");
        RuleFor(x => x.Placa)
            .MaximumLength(Unidad.PlacaMaxLength).WithMessage($"La placa no puede exceder {Unidad.PlacaMaxLength} caracteres.");
        RuleFor(x => x.DenominacionId).GreaterThan(0).WithMessage("La denominación es requerida.");
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

        var unidad = Unidad.Crear(request.Ficha, request.Placa);

        if (await unidades.ExisteFichaAsync(unidad.Ficha, cancellationToken: cancellationToken))
            throw new ConflictException($"La ficha '{unidad.Ficha}' ya está registrada en el sistema.");

        var denominacion = await denominaciones.GetByIdAsync(request.DenominacionId, cancellationToken)
            ?? throw new NotFoundException("La denominación", request.DenominacionId);

        var ocupantes = await unidades.GetActivasConDenominacionAsync(denominacion.Id, cancellationToken);
        AsignacionDenominacionService.Asignar(unidad, denominacion, ocupantes, autor);

        unidades.Add(unidad);
        await uow.SaveChangesAsync(cancellationToken);

        return unidad.Id;
    }
}
