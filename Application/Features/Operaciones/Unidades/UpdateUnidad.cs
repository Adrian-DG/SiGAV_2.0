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

// Command
public record UpdateUnidadCommand(int UnidadId, string Ficha, string? Placa, int DenominacionId, int NivelDenominacionId, string? Motivo = null) : IRequest;

// Validator
public class UpdateUnidadCommandValidator : AbstractValidator<UpdateUnidadCommand>
{
    public UpdateUnidadCommandValidator(IReadDbContext db)
    {
        RuleFor(x => x.UnidadId).GreaterThan(0).WithMessage("La unidad es requerida.");
        RuleFor(x => x.Ficha)
            .NotEmpty().WithMessage("La ficha es requerida.")
            .MaximumLength(Unidad.FichaMaxLength).WithMessage($"La ficha no puede exceder {Unidad.FichaMaxLength} caracteres.");
        RuleFor(x => x.Placa)
            .MaximumLength(Unidad.PlacaMaxLength).WithMessage($"La placa no puede exceder {Unidad.PlacaMaxLength} caracteres.");
        RuleFor(x => x.DenominacionId).GreaterThan(0).WithMessage("La denominación es requerida.");
        RuleFor(x => x.NivelDenominacionId)
            .GreaterThan(0).WithMessage("El nivel de la denominación es requerido.")
            .MustAsync((id, ct) => db.NivelesDenominacion.ExisteActivoAsync(id, ct)).WithMessage("El nivel de denominación especificado no existe.");
        RuleFor(x => x.Motivo)
            .MaximumLength(AutorCambio.ObservacionMaxLength).WithMessage($"El motivo no puede exceder {AutorCambio.ObservacionMaxLength} caracteres.");
    }
}

// Handler
public class UpdateUnidadCommandHandler(
    IUnidadRepository unidades,
    IDenominacionRepository denominaciones,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IRequestHandler<UpdateUnidadCommand, Unit>
{
    public async Task<Unit> Handle(UpdateUnidadCommand request, CancellationToken cancellationToken)
    {
        var autor = currentUser.RequerirAutorWeb(timeProvider, request.Motivo);

        var unidad = await unidades.GetByIdAsync(request.UnidadId, cancellationToken)
            ?? throw new NotFoundException("La unidad", request.UnidadId);

        var denominacion = await denominaciones.GetByIdAsync(request.DenominacionId, cancellationToken)
            ?? throw new NotFoundException("La denominación", request.DenominacionId);

        var ficha = Unidad.NormalizarFicha(request.Ficha);
        if (await unidades.ExisteFichaAsync(ficha, excluirUnidadId: unidad.Id, cancellationToken))
            throw new ConflictException($"La ficha '{ficha}' ya está registrada en otra unidad.");

        unidad.ActualizarDatos(ficha, request.Placa);

        // El nivel vive en la denominación (en SiGAV 1.0 el tipo se duplicaba en la unidad)
        if (denominacion.NivelDenominacionId != request.NivelDenominacionId)
        {
            var nivel = await denominaciones.GetNivelAsync(request.NivelDenominacionId, cancellationToken)
                ?? throw new NotFoundException("El nivel de denominación", request.NivelDenominacionId);
            denominacion.CambiarNivel(nivel);
        }

        // Si cambia de denominación se libera de cualquier otra unidad que la tenga;
        // si es la misma, solo se vuelve a marcar disponible (comportamiento de SiGAV 1.0).
        List<Unidad> ocupantes = unidad.TieneDenominacion(denominacion.Id)
            ? []
            : await unidades.GetActivasConDenominacionAsync(denominacion.Id, cancellationToken);

        AsignacionDenominacionService.Asignar(unidad, denominacion, ocupantes, autor);

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
