using Application.Common;
using Application.Contracts;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Eventos;

/// <summary>
/// Un evento en el mapa. Categorias: un evento con tipos de ambas categorías aparece en las dos
/// capas (accidentes y asistencias). Ficha: la unidad principal. Fecha en UTC.
/// </summary>
public record PuntoMapaViewModel(
    int Id,
    decimal Latitud,
    decimal Longitud,
    IReadOnlyList<CategoriaEventoEnum> Categorias,
    IReadOnlyList<string> Tipos,
    DateTime FechaHoraReporte,
    string? Ficha);

/// <param name="Truncado">true si había más de <see cref="GetMapaEventosQueryValidator.MaxPuntos"/> eventos (se devuelven los más recientes).</param>
public record MapaEventosViewModel(DateOnly Desde, DateOnly Hasta, int Total, bool Truncado, IReadOnlyList<PuntoMapaViewModel> Puntos);

// Query: ubicación de los eventos activos del período (días operativos de RD), para el mapa de calor
// del front desk. Por defecto los últimos 30 días.
public record GetMapaEventosQuery(DateOnly? Desde = null, DateOnly? Hasta = null) : IRequest<MapaEventosViewModel>;

public class GetMapaEventosQueryValidator : AbstractValidator<GetMapaEventosQuery>
{
    public const int MaxDiasRango = 366;
    public const int MaxPuntos = 20_000;

    public GetMapaEventosQueryValidator()
    {
        RuleFor(x => x.Desde)
            .Must((q, desde) => desde is null || q.Hasta is null || desde <= q.Hasta)
            .WithMessage("La fecha inicial no puede ser posterior a la final.");
        RuleFor(x => x.Hasta)
            .Must((q, hasta) => q.Desde is null || hasta is null || hasta.Value.DayNumber - q.Desde.Value.DayNumber < MaxDiasRango)
            .WithMessage($"El rango de fechas no puede exceder {MaxDiasRango} días.");
    }
}

public class GetMapaEventosQueryHandler(IReadDbContext db, TimeProvider timeProvider)
    : IRequestHandler<GetMapaEventosQuery, MapaEventosViewModel>
{
    public async Task<MapaEventosViewModel> Handle(GetMapaEventosQuery request, CancellationToken cancellationToken)
    {
        var hasta = request.Hasta ?? ZonaHorariaOperativa.Hoy(timeProvider);
        var desde = request.Desde ?? hasta.AddDays(-29);
        var desdeUtc = ZonaHorariaOperativa.InicioDelDiaUtc(desde);
        var hastaExclusivoUtc = ZonaHorariaOperativa.InicioDelDiaUtc(hasta.AddDays(1));

        var query = db.Eventos.Where(e => e.IsActive && e.FechaHoraReporteUtc >= desdeUtc && e.FechaHoraReporteUtc < hastaExclusivoUtc);

        var total = await query.CountAsync(cancellationToken);
        var filas = await query
            .OrderByDescending(e => e.FechaHoraReporteUtc)
            .Take(GetMapaEventosQueryValidator.MaxPuntos)
            .Select(e => new
            {
                e.Id,
                e.Ubicacion.Latitud,
                e.Ubicacion.Longitud,
                Tipos = e.Tipos.Select(t => new { t.TipoEvento!.Nombre, t.TipoEvento.Categoria }).ToList(),
                e.FechaHoraReporteUtc,
                Ficha = e.Unidades.Where(u => u.Rol == RolUnidadEventoEnum.Principal).Select(u => u.Unidad!.Ficha).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var puntos = filas
            .Select(f => new PuntoMapaViewModel(
                f.Id,
                f.Latitud,
                f.Longitud,
                f.Tipos.Select(t => t.Categoria).Distinct().Order().ToList(),
                f.Tipos.Select(t => t.Nombre).Order().ToList(),
                f.FechaHoraReporteUtc,
                f.Ficha))
            .ToList();

        return new MapaEventosViewModel(desde, hasta, total, total > puntos.Count, puntos);
    }
}
