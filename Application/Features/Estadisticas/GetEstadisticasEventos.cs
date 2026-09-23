using Application.Common;
using Application.Contracts;
using Application.Contracts.Authentication;
using Application.Contracts.Operaciones;
using Application.Exceptions;
using Domain.Repositories;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Application.Features.Estadisticas;

// Query: eventos atendidos por tipo y categoría dentro del alcance de la sesión.
//  - Front desk (web): todas las regiones.
//  - App móvil: según la jerarquía de la denominación que tiene HOY la unidad de la sesión.
public record GetEstadisticasEventosQuery(DateOnly? Desde = null, DateOnly? Hasta = null) : IRequest<EstadisticasEventosViewModel>;

// Validator
public class GetEstadisticasEventosQueryValidator : AbstractValidator<GetEstadisticasEventosQuery>
{
    public const int MaxDiasRango = 366;

    public GetEstadisticasEventosQueryValidator()
    {
        RuleFor(x => x.Desde)
            .Must((q, desde) => desde is null || q.Hasta is null || desde <= q.Hasta)
            .WithMessage("La fecha inicial no puede ser posterior a la final.");
        RuleFor(x => x.Hasta)
            .Must((q, hasta) => q.Desde is null || hasta is null || hasta.Value.DayNumber - q.Desde.Value.DayNumber < MaxDiasRango)
            .WithMessage($"El rango de fechas no puede exceder {MaxDiasRango} días.");
    }
}

// Handler
public class GetEstadisticasEventosQueryHandler(
    ICurrentUserService currentUser,
    IUnidadRepository unidades,
    IDenominacionRepository denominaciones,
    IEstadisticasQueries queries,
    TimeProvider timeProvider) : IRequestHandler<GetEstadisticasEventosQuery, EstadisticasEventosViewModel>
{
    public async Task<EstadisticasEventosViewModel> Handle(GetEstadisticasEventosQuery request, CancellationToken cancellationToken)
    {
        // "Hoy" en hora operativa (RD), no en la zona del servidor
        var hoy = ZonaHorariaOperativa.Hoy(timeProvider);
        var hasta = request.Hasta ?? hoy;
        // Por defecto: desde el inicio del mes de la fecha final
        var desde = request.Desde ?? new DateOnly(hasta.Year, hasta.Month, 1);

        var alcance = await ResolverAlcanceAsync(cancellationToken);

        return await queries.GetEstadisticasEventosAsync(alcance, desde, hasta, cancellationToken);
    }

    private async Task<AlcanceEstadistico> ResolverAlcanceAsync(CancellationToken cancellationToken)
    {
        if (currentUser.TipoSesion == TiposSesion.Web) return AlcanceEstadistico.Global();

        if (currentUser.TipoSesion != TiposSesion.Movil || currentUser.UnidadId is not { } unidadId)
            throw new ForbiddenException("La sesión actual no tiene acceso a estadísticas.");

        // Se consulta la denominación vigente (no la del token): si la unidad fue reasignada
        // el alcance cambia de inmediato, sin esperar a que el token expire.
        var unidad = await unidades.GetByIdAsync(unidadId, cancellationToken);
        if (unidad is null || !unidad.IsActive || unidad.DenominacionId is not { } denominacionId)
            throw new ForbiddenException("La unidad de la sesión no está activa o no tiene denominación asignada.");

        var denominacion = await denominaciones.GetByIdAsync(denominacionId, cancellationToken)
            ?? throw new ForbiddenException("La denominación de la unidad no está activa.");

        return denominacion.ObtenerAlcance(unidad.Id);
    }
}
