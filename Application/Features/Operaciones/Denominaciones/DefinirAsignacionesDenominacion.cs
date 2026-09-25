using Application.Common;
using Application.Contracts;
using Application.Exceptions;
using Domain.Enums;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Denominaciones;

// Command: reemplaza las regiones/tramos que supervisa una denominación de encargado.
//  - Regional: RegionesMacro y/o RegionesAsistencia
//  - Tramo:    Tramos (su propio tramo siempre está incluido)
//  - Unidad:   no admite asignaciones (enviar listas vacías para limpiar)
public record DefinirAsignacionesDenominacionCommand(
    int DenominacionId,
    IReadOnlyList<RegionMacroEnum>? RegionesMacro,
    IReadOnlyList<int>? RegionesAsistencia,
    IReadOnlyList<int>? Tramos) : IRequest;

// Validator
public class DefinirAsignacionesDenominacionCommandValidator : AbstractValidator<DefinirAsignacionesDenominacionCommand>
{
    private const int MaxAsignaciones = 100;

    public DefinirAsignacionesDenominacionCommandValidator(IReadDbContext db)
    {
        RuleFor(x => x.DenominacionId).GreaterThan(0).WithMessage("La denominación es requerida.");

        RuleForEach(x => x.RegionesMacro)
            .IsInEnum().WithMessage("Hay regiones macro no válidas.");

        RuleFor(x => x.RegionesAsistencia)
            .Must(r => r is null || r.Count <= MaxAsignaciones).WithMessage($"No se pueden asignar más de {MaxAsignaciones} regiones.")
            .MustAsync((ids, ct) => db.Regiones.ExistenActivosAsync(ids, ct))
            .WithMessage("Una o más regiones de asistencia no existen.");

        RuleFor(x => x.Tramos)
            .Must(t => t is null || t.Count <= MaxAsignaciones).WithMessage($"No se pueden asignar más de {MaxAsignaciones} tramos.")
            .MustAsync((ids, ct) => db.Tramos.ExistenActivosAsync(ids, ct))
            .WithMessage("Uno o más tramos no existen.");
    }
}

// Handler
public class DefinirAsignacionesDenominacionCommandHandler(IDenominacionRepository denominaciones, IUnitOfWork uow)
    : IRequestHandler<DefinirAsignacionesDenominacionCommand, Unit>
{
    public async Task<Unit> Handle(DefinirAsignacionesDenominacionCommand request, CancellationToken cancellationToken)
    {
        var denominacion = await denominaciones.GetByIdAsync(request.DenominacionId, cancellationToken)
            ?? throw new NotFoundException("La denominación", request.DenominacionId);

        // Las reglas de qué jerarquía admite qué asignación viven en el agregado
        denominacion.DefinirAsignaciones(
            request.RegionesMacro ?? [],
            request.RegionesAsistencia ?? [],
            request.Tramos ?? []);

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
