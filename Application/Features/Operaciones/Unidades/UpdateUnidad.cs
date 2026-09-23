using Application.Contracts.Operaciones;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Repositories;
using Domain.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

// Command
public record UpdateUnidadCommand(int UnidadId, string Ficha, string? Placa, int DenominacionId, int TipoUnidadId) : IRequest;

// Validator
public class UpdateUnidadCommandValidator : AbstractValidator<UpdateUnidadCommand>
{
    public UpdateUnidadCommandValidator(ICatalogoQueries catalogos)
    {
        RuleFor(x => x.UnidadId).GreaterThan(0).WithMessage("La unidad es requerida.");
        RuleFor(x => x.Ficha)
            .NotEmpty().WithMessage("La ficha es requerida.")
            .MaximumLength(Unidad.FichaMaxLength).WithMessage($"La ficha no puede exceder {Unidad.FichaMaxLength} caracteres.");
        RuleFor(x => x.Placa)
            .MaximumLength(Unidad.PlacaMaxLength).WithMessage($"La placa no puede exceder {Unidad.PlacaMaxLength} caracteres.");
        RuleFor(x => x.DenominacionId).GreaterThan(0).WithMessage("La denominación es requerida.");
        RuleFor(x => x.TipoUnidadId)
            .GreaterThan(0).WithMessage("El tipo de unidad es requerido.")
            .MustAsync(catalogos.ExisteTipoUnidadAsync).WithMessage("El tipo de unidad especificado no existe.");
    }
}

// Handler
public class UpdateUnidadCommandHandler(
    IUnidadRepository unidades,
    IDenominacionRepository denominaciones,
    IUnitOfWork uow) : IRequestHandler<UpdateUnidadCommand, Unit>
{
    public async Task<Unit> Handle(UpdateUnidadCommand request, CancellationToken cancellationToken)
    {
        var unidad = await unidades.GetByIdAsync(request.UnidadId, cancellationToken)
            ?? throw new NotFoundException("La unidad", request.UnidadId);

        var denominacion = await denominaciones.GetByIdAsync(request.DenominacionId, cancellationToken)
            ?? throw new NotFoundException("La denominación", request.DenominacionId);

        var ficha = Unidad.NormalizarFicha(request.Ficha);
        if (await unidades.ExisteFichaAsync(ficha, excluirUnidadId: unidad.Id, cancellationToken))
            throw new ConflictException($"La ficha '{ficha}' ya está registrada en otra unidad.");

        unidad.ActualizarDatos(ficha, request.Placa);

        // El tipo de unidad vive en la denominación (en SiGAV 1.0 se duplicaba en la unidad)
        if (denominacion.TipoUnidadId != request.TipoUnidadId)
            denominacion.CambiarTipoUnidad(request.TipoUnidadId);

        // Si cambia de denominación se libera de cualquier otra unidad que la tenga;
        // si es la misma, solo se vuelve a marcar disponible (comportamiento de SiGAV 1.0).
        List<Unidad> ocupantes = unidad.TieneDenominacion(denominacion.Id)
            ? []
            : await unidades.GetActivasConDenominacionAsync(denominacion.Id, cancellationToken);

        AsignacionDenominacionService.Asignar(unidad, denominacion, ocupantes);

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
