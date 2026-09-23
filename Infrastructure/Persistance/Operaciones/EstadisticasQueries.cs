using Application.Common;
using Application.Contracts.Operaciones;
using Application.Features.Estadisticas;
using Application.Features.Operaciones.Denominaciones;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

/// <summary>
/// Se traen filas livianas (solo Ids) acotadas por fecha y alcance y se agregan en memoria:
/// así cada nivel (región, tramo, unidad) cuenta eventos distintos sin duplicar los que
/// atendieron varias unidades o tienen varios tipos.
/// </summary>
public class EstadisticasQueries(SiGAVContext context) : IEstadisticasQueries
{
    private sealed record Fila(int EventoId, int UnidadId, int DenominacionId, int TramoId, int? TipoEventoId);
    private sealed record TipoInfo(string Nombre, CategoriaEventoEnum Categoria);
    private sealed record TramoInfo(string Nombre, int RegionAsistenciaId);
    private sealed record RegionInfo(string Nombre, RegionMacroEnum Macro);

    public async Task<EstadisticasEventosViewModel> GetEstadisticasEventosAsync(
        AlcanceEstadistico alcance,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        var tramosAlcance = await ResolverTramosAsync(alcance, cancellationToken);
        var alcanceViewModel = await ConstruirAlcanceAsync(alcance, tramosAlcance, cancellationToken);

        if (alcance.EstaVacio || tramosAlcance is { Count: 0 })
            return new EstadisticasEventosViewModel(alcanceViewModel, desde, hasta, ResumenVacio, []);

        // Por fecha del reporte (no la de guardado: la app sincroniza eventos offline tarde)
        var (desdeUtc, hastaExclusivoUtc) = ZonaHorariaOperativa.RangoUtc(desde, hasta);
        var participaciones = context.EventoUnidades
            .AsNoTracking()
            .Where(eu => eu.Evento!.IsActive && eu.Evento.FechaHoraReporteUtc >= desdeUtc && eu.Evento.FechaHoraReporteUtc < hastaExclusivoUtc);

        if (alcance.Jerarquia == JerarquiaEnum.Unidad)
            participaciones = participaciones.Where(eu => eu.UnidadId == alcance.UnidadId);
        else if (tramosAlcance is not null)
            participaciones = participaciones.Where(eu => tramosAlcance.Contains(eu.Denominacion!.TramoId));

        var filas = await (
                from eu in participaciones
                join et in context.EventoTiposEvento on eu.EventoId equals et.EventoId into tipos
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

        var tiposInfo = await context.TipoEventos.AsNoTracking()
            .Where(t => tipoIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => new TipoInfo(t.Nombre, t.Categoria), cancellationToken);
        var fichas = await context.Unidades.AsNoTracking()
            .Where(u => unidadIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Ficha, cancellationToken);
        var denominaciones = await context.Denominaciones.AsNoTracking()
            .Where(d => denominacionIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Nombre, cancellationToken);
        var tramos = await context.Tramos.AsNoTracking()
            .Where(t => tramoIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => new TramoInfo(t.Nombre, t.RegionAsistenciaId), cancellationToken);
        var regionIds = tramos.Values.Select(t => t.RegionAsistenciaId).Distinct().ToList();
        var regiones = await context.Regiones.AsNoTracking()
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

    private static readonly ResumenEventosViewModel ResumenVacio = new(0, [], []);

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
                return await context.Tramos.AsNoTracking()
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
            ? await context.Denominaciones.AsNoTracking()
                .Where(d => d.Id == denominacionId)
                .Select(d => d.Nombre)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var regionIds = alcance.RegionesAsistencia.ToList();
        var regiones = regionIds.Count == 0
            ? []
            : await context.Regiones.AsNoTracking()
                .Where(r => regionIds.Contains(r.Id))
                .OrderBy(r => r.Nombre)
                .Select(r => new AsignacionViewModel(r.Id, r.Nombre))
                .ToListAsync(cancellationToken);

        var tramos = tramosAlcance is not { Count: > 0 }
            ? []
            : await context.Tramos.AsNoTracking()
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
