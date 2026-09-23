using Application.Common;
using Application.Contracts;
using Application.Contracts.Operaciones;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Repositories;
using Domain.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

// Command: crea la denominación y la unidad en una sola operación atómica
public record CreateUnidadConNuevaDenominacionCommand(
    string Ficha,
    string? Placa,
    string Denominacion,
    int TramoId,
    int NivelDenominacionId) : IRequest<int>;

// Validator
public class CreateUnidadConNuevaDenominacionCommandValidator : AbstractValidator<CreateUnidadConNuevaDenominacionCommand>
{
    public CreateUnidadConNuevaDenominacionCommandValidator(ICatalogoQueries catalogos)
    {
        RuleFor(x => x.Ficha)
            .NotEmpty().WithMessage("La ficha es requerida.")
            .MaximumLength(Unidad.FichaMaxLength).WithMessage($"La ficha no puede exceder {Unidad.FichaMaxLength} caracteres.");
        RuleFor(x => x.Placa)
            .MaximumLength(Unidad.PlacaMaxLength).WithMessage($"La placa no puede exceder {Unidad.PlacaMaxLength} caracteres.");
        RuleFor(x => x.Denominacion)
            .NotEmpty().WithMessage("La denominación es requerida.")
            .MaximumLength(Domain.Entities.Operaciones.Denominacion.NombreMaxLength)
            .WithMessage($"La denominación no puede exceder {Domain.Entities.Operaciones.Denominacion.NombreMaxLength} caracteres.");
        RuleFor(x => x.TramoId)
            .GreaterThan(0).WithMessage("El tramo es requerido.")
            .MustAsync(catalogos.ExisteTramoAsync).WithMessage("El tramo especificado no existe.");
        RuleFor(x => x.NivelDenominacionId)
            .GreaterThan(0).WithMessage("El nivel de la denominación es requerido.")
            .MustAsync(catalogos.ExisteNivelDenominacionAsync).WithMessage("El nivel de denominación especificado no existe.");
    }
}

// Handler
public class CreateUnidadConNuevaDenominacionCommandHandler(
    IUnidadRepository unidades,
    IDenominacionRepository denominaciones,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IRequestHandler<CreateUnidadConNuevaDenominacionCommand, int>
{
    public async Task<int> Handle(CreateUnidadConNuevaDenominacionCommand request, CancellationToken cancellationToken)
    {
        var autor = currentUser.RequerirAutorWeb(timeProvider);

        var unidad = Unidad.Crear(request.Ficha, request.Placa);
        var denominacion = Denominacion.Crear(request.Denominacion, request.TramoId, request.NivelDenominacionId);

        if (await unidades.ExisteFichaAsync(unidad.Ficha, cancellationToken: cancellationToken))
            throw new ConflictException($"La ficha '{unidad.Ficha}' ya está registrada en el sistema.");

        if (await denominaciones.ExisteNombreAsync(denominacion.Nombre, cancellationToken))
            throw new ConflictException($"La denominación '{denominacion.Nombre}' ya está registrada en el sistema.");

        // Denominación nueva: no hay unidades que la ocupen
        AsignacionDenominacionService.Asignar(unidad, denominacion, [], autor);

        denominaciones.Add(denominacion);
        unidades.Add(unidad);

        // Un único SaveChanges es transaccional: se insertan ambas o ninguna
        await uow.SaveChangesAsync(cancellationToken);

        return unidad.Id;
    }
}
