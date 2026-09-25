using Application.Common;
using Application.Contracts;
using Application.Contracts.Authentication;
using Application.Exceptions;
using Application.Features.Operaciones.Denominaciones;
using Domain.Enums;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

// Handler: se traen filas livianas (solo Ids) acotadas por fecha y alcance y se agregan en
// memoria; así cada nivel (región, tramo, unidad) cuenta eventos distintos sin duplicar los que
// atendieron varias unidades o tienen varios tipos.
public class GetEstadisticasEventosQueryHandler(
    ICurrentUserService currentUser,
    IReadDbContext db,
    TimeProvider timeProvider) : IRequestHandler<GetEstadisticasEventosQuery, EstadisticasEventosViewModel>
{
    private sealed record Fila(int EventoId, int UnidadId, int DenominacionId, int TramoId, int? TipoEventoId);
    private sealed record TipoInfo(string Nombre, CategoriaEventoEnum Categoria);
    private sealed record TramoInfo(string Nombre, int RegionAsistenciaId);
    private sealed record RegionInfo(string Nombre, RegionMacroEnum Macro);

    private static readonly ResumenEventosViewModel ResumenVacio = new(0, [], []);

    public async Task<EstadisticasEventosViewModel> Handle(GetEstadisticasEventosQuery request, CancellationToken cancellationToken)
    {
        // "Hoy" en hora operativa (RD), no en la zona del servidor
        var hoy = ZonaHorariaOperativa.Hoy(timeProvider);
        var hasta = request.Hasta ?? hoy;
        // Por defecto: desde el inicio del mes de la fecha final
        var desde = request.Desde ?? new DateOnly(hasta.Year, hasta.Month, 1);

        var alcance = await ResolverAlcanceAsync(cancellationToken);
        var tramosAlcance = await ResolverTramosAsync(alcance, cancellationToken);
        var alcanceViewModel = await ConstruirAlcanceAsync(alcance, tramosAlcance, cancellationToken);

        if (alcance.EstaVacio || tramosAlcance is { Count: 0 })
            return new EstadisticasEventosViewModel(alcanceViewModel, desde, hasta, ResumenVacio, []);

        // Por fecha del reporte (no la de guardado: la app sincroniza eventos offline tarde)
        var (desdeUtc, hastaExclusivoUtc) = ZonaHorariaOperativa.RangoUtc(desde, hasta);
        var participaciones = db.EventoUnidades
            .Where(eu => eu.Evento!.IsActive && eu.Evento.FechaHoraReporteUtc >= desdeUtc && eu.Evento.FechaHoraReporteUtc < hastaExclusivoUtc);

        if (alcance.Jerarquia == JerarquiaEnum.Unidad)
            participaciones = participaciones.Where(eu => eu.UnidadId == alcance.UnidadId);
        else if (tramosAlcance is not null)
            participaciones = participaciones.Where(eu => tramosAlcance.Contains(eu.Denominacion!.TramoId));

        var filas = await (
                from eu in participaciones
                join et in db.EventoTiposEvento on eu.EventoId equals et.EventoId into tipos
                from et in tipos.DefaultIfEmpty()
                select new Fila(eu.EventoId, eu.UnidadId, eu.DenominacionId, eu.Denominacion!.TramoId, (int?)et!.TipoEventoId))
            .ToListAsync(cancellationToken);

        if (filas.Count == 0)
            return new EstadisticasEventosViewModel(alcanceViewModel, desde, hasta, ResumenVacio, []);

        // Catálogos solo de lo que aparece en el resultado
        var tipoIds = filas.Where(f => f.TipoEventoId.HasValue).Select(f => f.TipoEventoId!.Value).Distinct().ToList();
        var unidadIds = filas.Select(f => f.UnidadId).Distinct().ToList();
        var denominacionIds = filas.Select(f => f.DenominacionId).Distinct().ToList();
        var tramoIds = filas.Select(f => f.TramoId).Distinct().ToList();

        var tiposInfo = await db.TiposEvento
            .Where(t => tipoIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => new TipoInfo(t.Nombre, t.Categoria), cancellationToken);
        var fichas = await db.Unidades
            .Where(u => unidadIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Ficha, cancellationToken);
        var denominaciones = await db.Denominaciones
            .Where(d => denominacionIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Nombre, cancellationToken);
        var tramos = await db.Tramos
            .Where(t => tramoIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => new TramoInfo(t.Nombre, t.RegionAsistenciaId), cancellationToken);
        var regionIds = tramos.Values.Select(t => t.RegionAsistenciaId).Distinct().ToList();
        var regiones = await db.Regiones
            .Where(r => regionIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => new RegionInfo(r.Nombre, r.Region), cancellationToken);

        ResumenEventosViewModel Resumir(IReadOnlyCollection<Fila> grupo)
        {
            var conTipo = grupo.Where(f => f.TipoEventoId.HasValue && tiposInfo.ContainsKey(f.TipoEventoId.Value)).ToList();

            var porCategoria = conTipo
                .GroupBy(f => tiposInfo[f.TipoEventoId!.Value].Categoria)
                .Select(g => new CategoriaTotalViewModel(g.Key, g.Select(f => f.EventoId).Distinct().Count()))
                .OrderBy(c => c.Categoria)
                .ToList();

            var porTipo = conTipo
                .GroupBy(f => f.TipoEventoId!.Value)
                .Select(g => new TipoEventoTotalViewModel(
                    g.Key,
                    tiposInfo[g.Key].Nombre,
                    tiposInfo[g.Key].Categoria,
                    g.Select(f => f.EventoId).Distinct().Count()))
                .OrderByDescending(t => t.Total)
                .ThenBy(t => t.TipoEvento)
                .ToList();

            return new ResumenEventosViewModel(grupo.Select(f => f.EventoId).Distinct().Count(), porCategoria, porTipo);
        }

        var arbol = filas
            .GroupBy(f => tramos.TryGetValue(f.TramoId, out var t) ? t.RegionAsistenciaId : 0)
            .Select(porRegion =>
            {
                var region = regiones.GetValueOrDefault(porRegion.Key);
                var filasRegion = porRegion.ToList();

                var tramosRegion = filasRegion
                    .GroupBy(f => f.TramoId)
                    .Select(porTramo =>
                    {
                        var filasTramo = porTramo.ToList();
                        var unidades = filasTramo
                            .GroupBy(f => (f.UnidadId, f.DenominacionId))
                            .Select(porUnidad => new UnidadEstadisticaViewModel(
                                porUnidad.Key.UnidadId,
                                fichas.GetValueOrDefault(porUnidad.Key.UnidadId, string.Empty),
                                porUnidad.Key.DenominacionId,
                                denominaciones.GetValueOrDefault(porUnidad.Key.DenominacionId, string.Empty),
                                Resumir(porUnidad.ToList())))
                            .OrderByDescending(u => u.Resumen.TotalEventos)
                            .ThenBy(u => u.Ficha)
                            .ToList();

                        return new TramoEstadisticaViewModel(
                            porTramo.Key,
                            tramos.GetValueOrDefault(porTramo.Key)?.Nombre ?? string.Empty,
                            Resumir(filasTramo),
                            unidades);
                    })
                    .OrderBy(t => t.Tramo)
                    .ToList();

                return new RegionEstadisticaViewModel(
                    porRegion.Key,
                    region?.Nombre ?? string.Empty,
                    region?.Macro ?? default,
                    Resumir(filasRegion),
                    tramosRegion);
            })
            .OrderBy(r => r.RegionMacro)
            .ThenBy(r => r.Region)
            .ToList();

        return new EstadisticasEventosViewModel(alcanceViewModel, desde, hasta, Resumir(filas), arbol);
    }

    private async Task<AlcanceEstadistico> ResolverAlcanceAsync(CancellationToken cancellationToken)
    {
        if (currentUser.TipoSesion == TiposSesion.Web) return AlcanceEstadistico.Global();

        if (currentUser.TipoSesion != TiposSesion.Movil || currentUser.UnidadId is not { } unidadId)
            throw new ForbiddenException("La sesión actual no tiene acceso a estadísticas.");

        // Se consulta la denominación vigente (no la del token): si la unidad fue reasignada
        // el alcance cambia de inmediato, sin esperar a que el token expire.
        var denominacionId = await db.Unidades
            .Where(u => u.Id == unidadId && u.IsActive)
            .Select(u => u.DenominacionId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("La unidad de la sesión no está activa o no tiene denominación asignada.");

        // La regla de qué ve cada jerarquía es del dominio (Denominacion.ObtenerAlcance)
        var denominacion = await db.Denominaciones
            .Include(d => d.Nivel)
            .Include(d => d.Regiones)
            .Include(d => d.Tramos)
            .FirstOrDefaultAsync(d => d.Id == denominacionId && d.IsActive, cancellationToken)
            ?? throw new ForbiddenException("La denominación de la unidad no está activa.");

        return denominacion.ObtenerAlcance(unidadId);
    }

    /// <summary>
    /// Tramos cubiertos por el alcance. null = sin restricción por tramo (global, o una unidad,
    /// que se filtra por su ficha).
    /// </summary>
    private async Task<List<int>?> ResolverTramosAsync(AlcanceEstadistico alcance, CancellationToken cancellationToken)
    {
        switch (alcance.Jerarquia)
        {
            case JerarquiaEnum.Regional:
                var macros = alcance.RegionesMacro.ToList();
                var regiones = alcance.RegionesAsistencia.ToList();
                // Incluye tramos inactivos: sus eventos históricos siguen contando
                return await db.Tramos
                    .Where(t => regiones.Contains(t.RegionAsistenciaId) || macros.Contains(t.RegionAsistencia!.Region))
                    .Select(t => t.Id)
                    .ToListAsync(cancellationToken);
            case JerarquiaEnum.Tramo:
                return alcance.Tramos.ToList();
            default:
                return null;
        }
    }

    private async Task<AlcanceViewModel> ConstruirAlcanceAsync(AlcanceEstadistico alcance, List<int>? tramosAlcance, CancellationToken cancellationToken)
    {
        var denominacion = alcance.DenominacionId is { } denominacionId
            ? await db.Denominaciones
                .Where(d => d.Id == denominacionId)
                .Select(d => d.Nombre)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var regionIds = alcance.RegionesAsistencia.ToList();
        var regiones = regionIds.Count == 0
            ? []
            : await db.Regiones
                .Where(r => regionIds.Contains(r.Id))
                .OrderBy(r => r.Nombre)
                .Select(r => new AsignacionViewModel(r.Id, r.Nombre))
                .ToListAsync(cancellationToken);

        var tramos = tramosAlcance is not { Count: > 0 }
            ? []
            : await db.Tramos
                .Where(t => tramosAlcance.Contains(t.Id))
                .OrderBy(t => t.Nombre)
                .Select(t => new AsignacionViewModel(t.Id, t.Nombre))
                .ToListAsync(cancellationToken);

        return new AlcanceViewModel(
            alcance.Jerarquia,
            alcance.DenominacionId,
            denominacion,
            alcance.RegionesMacro.Order().ToList(),
            regiones,
            tramos);
    }
}
