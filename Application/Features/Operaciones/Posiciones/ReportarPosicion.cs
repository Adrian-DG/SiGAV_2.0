using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Repositories;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Posiciones;

/// <summary>
/// Posición actual de la unidad, enviada por la app móvil cada ~30 s mientras hay sesión.
/// La unidad y el agente salen del token, nunca del cuerpo.
/// </summary>
public record ReportarPosicionCommand(
    decimal Latitud,
    decimal Longitud,
    double? PrecisionMetros,
    double? Rumbo,
    double? VelocidadKmh,
    DateTime FechaHoraGpsUtc) : IRequest<Unit>;

// Validator
public class ReportarPosicionCommandValidator : AbstractValidator<ReportarPosicionCommand>
{
    public ReportarPosicionCommandValidator()
    {
        RuleFor(x => x.Latitud).InclusiveBetween(-90, 90).WithMessage("La latitud debe estar entre -90 y 90.");
        RuleFor(x => x.Longitud).InclusiveBetween(-180, 180).WithMessage("La longitud debe estar entre -180 y 180.");
        RuleFor(x => x).Must(x => x.Latitud != 0 || x.Longitud != 0)
            .WithName("Ubicacion").WithMessage("La coordenada (0, 0) no es una ubicación válida.");
        RuleFor(x => x.PrecisionMetros).InclusiveBetween(0, 100_000).When(x => x.PrecisionMetros is not null)
            .WithMessage("La precisión no es válida.");
        RuleFor(x => x.Rumbo).InclusiveBetween(0, 360).When(x => x.Rumbo is not null)
            .WithMessage("El rumbo debe estar entre 0 y 360 grados.");
        RuleFor(x => x.VelocidadKmh).InclusiveBetween(0, 400).When(x => x.VelocidadKmh is not null)
            .WithMessage("La velocidad no es válida.");
        RuleFor(x => x.FechaHoraGpsUtc).NotEmpty().WithMessage("La fecha de la lectura es requerida.");
    }
}

// Handler
public class ReportarPosicionCommandHandler(
    ICurrentUserService currentUser,
    IUnidadRepository unidades,
    IUnidadPosicionRepository posiciones,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<ReportarPosicionCommand, Unit>
{
    public async Task<Unit> Handle(ReportarPosicionCommand request, CancellationToken cancellationToken)
    {
        var unidadId = currentUser.UnidadId ?? throw new ForbiddenException("La sesión no tiene una unidad asociada.");
        var agenteId = currentUser.AgenteId ?? throw new ForbiddenException("La sesión no tiene un agente asociado.");
        var ahoraUtc = timeProvider.GetUtcNow().UtcDateTime;

        // La sesión (JWT) no se invalida al cambiar la unidad: se revisa aquí en cada envío. 403
        // le indica a la app que deje de enviar (la unidad fue desactivada, marcada No disponible o
        // perdió su denominación por una reasignación).
        var unidad = await unidades.GetByIdAsync(unidadId, cancellationToken);
        if (unidad is null || !unidad.IsActive || !unidad.EstaDisponible || unidad.DenominacionId is null)
            throw new ForbiddenException("La unidad ya no está disponible para operar.");

        var lectura = new LecturaGps(
            new Coordenada(request.Latitud, request.Longitud),
            request.PrecisionMetros,
            request.Rumbo,
            request.VelocidadKmh,
            DateTime.SpecifyKind(request.FechaHoraGpsUtc.ToUniversalTime(), DateTimeKind.Utc));

        var posicion = await posiciones.GetByUnidadIdAsync(unidadId, cancellationToken);
        if (posicion is null)
        {
            posiciones.Add(UnidadPosicion.Crear(unidadId, agenteId, lectura, ahoraUtc));
        }
        else
        {
            // Envío de una sesión que ya se cerró (logout o front desk la liberó): la app debe salir
            if (currentUser.SesionEmitidaUtc is { } emitida && posicion.SesionCerradaParaToken(emitida))
                throw new UnauthorizedException("La sesión de la unidad fue cerrada. Inicie sesión nuevamente.");

            // Una sola sesión por unidad: el agente que la tomó primero la conserva mientras siga enviando
            if (posicion.OcupadaPorOtroAgente(agenteId, ahoraUtc))
                throw new ConflictException($"La unidad {unidad.Ficha} está siendo operada por otro agente.");
            if (!posicion.Registrar(agenteId, lectura, ahoraUtc)) return Unit.Value;
        }

        try
        {
            await uow.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            // Primer envío de la unidad llegando dos veces a la vez: el otro ya la registró
        }

        return Unit.Value;
    }
}
