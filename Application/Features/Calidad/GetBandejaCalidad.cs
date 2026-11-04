using Application.Common;
using Application.Common.Models;
using Application.Contracts;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Calidad;

/// <summary>
/// Bandeja de calidad: eventos completados (no anulados) con el estado de su evaluación, más
/// recientes primero. <see cref="Estado"/>: 0 = pendientes (sin evaluación), o un
/// <see cref="EstadoEvaluacionEnum"/>. Las fechas son días operativos de RD (fecha de reporte).
/// Ciudadano: cédula, nombre o teléfono de una persona del evento.
/// </summary>
public record GetBandejaCalidadQuery(
    int? Estado = null,
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    string? Ciudadano = null,
    int Page = 1,
    int Size = 20) : IRequest<PagedResult<BandejaCalidadItemViewModel>>, IPagedQuery;

public class GetBandejaCalidadQueryValidator : PagedQueryValidator<GetBandejaCalidadQuery>
{
    public const int Pendiente = 0;

    public GetBandejaCalidadQueryValidator()
    {
        RuleFor(x => x.Estado)
            .Must(e => e is null or Pendiente || Enum.IsDefined((EstadoEvaluacionEnum)e.Value))
            .WithMessage("El estado no es válido.");
        RuleFor(x => x.Desde)
            .Must((q, desde) => desde is null || q.Hasta is null || desde <= q.Hasta)
            .WithMessage("La fecha inicial no puede ser posterior a la final.");
        RuleFor(x => x.Ciudadano).MaximumLength(100).WithMessage("La búsqueda no puede exceder 100 caracteres.");
    }
}

public class GetBandejaCalidadQueryHandler(IReadDbContext db)
    : IRequestHandler<GetBandejaCalidadQuery, PagedResult<BandejaCalidadItemViewModel>>
{
    public async Task<PagedResult<BandejaCalidadItemViewModel>> Handle(GetBandejaCalidadQuery request, CancellationToken cancellationToken)
    {
        var query = db.Eventos.Where(e => e.IsActive && e.Estado == EstadoEventoEnum.Completado);

        if (request.Desde is { } desde)
        {
            var desdeUtc = ZonaHorariaOperativa.InicioDelDiaUtc(desde);
            query = query.Where(e => e.FechaHoraReporteUtc >= desdeUtc);
        }
        if (request.Hasta is { } hasta)
        {
            var hastaUtc = ZonaHorariaOperativa.InicioDelDiaUtc(hasta.AddDays(1));
            query = query.Where(e => e.FechaHoraReporteUtc < hastaUtc);
        }
        if (request.Estado is GetBandejaCalidadQueryValidator.Pendiente)
            query = query.Where(e => !db.EvaluacionesCalidad.Any(c => c.EventoId == e.Id));
        else if (request.Estado is { } estado)
            query = query.Where(e => db.EvaluacionesCalidad.Any(c => c.EventoId == e.Id && c.Estado == (EstadoEvaluacionEnum)estado));
        if (QueryableExtensions.PatronBusqueda(request.Ciudadano) is { } patron)
            query = query.Where(e => e.Ciudadanos.Any(c =>
                (c.Persona.Identificacion != null && EF.Functions.Like(c.Persona.Identificacion, patron))
                || (c.Persona.Telefono != null && EF.Functions.Like(c.Persona.Telefono, patron))
                || EF.Functions.Like((c.Persona.Nombre ?? "") + " " + (c.Persona.Apellido ?? ""), patron)));

        var pagina = await query
            .OrderByDescending(e => e.FechaHoraReporteUtc)
            .ThenByDescending(e => e.Id)
            .Select(e => new Fila(
                e.Id,
                e.FechaHoraReporteUtc,
                e.FechaHoraCompletadoUtc,
                e.Tipos.Select(t => t.TipoEvento!.Nombre).Distinct().ToList(),
                e.Unidades.Where(u => u.Rol == RolUnidadEventoEnum.Principal).Select(u => u.Unidad!.Ficha).FirstOrDefault(),
                e.Unidades.Where(u => u.Rol == RolUnidadEventoEnum.Principal).Select(u => u.Denominacion!.Nombre).FirstOrDefault(),
                e.Tramo != null ? e.Tramo.Nombre : null,
                // Principal: primero quien tenga teléfono, y entre ellos el primer conductor
                e.Ciudadanos
                    .OrderBy(c => c.Persona.Telefono == null ? 1 : 0)
                    .ThenBy(c => c.Rol == RolCiudadanoEnum.Conductor ? 0 : 1)
                    .ThenBy(c => c.Id)
                    .Select(c => new PersonaFila(c.Persona.Nombre, c.Persona.Apellido, c.Persona.Identificacion, c.Persona.Telefono))
                    .FirstOrDefault(),
                e.Ciudadanos.Count(c => c.Persona.Telefono != null),
                db.EvaluacionesCalidad
                    .Where(c => c.EventoId == e.Id)
                    .Select(c => new EvaluacionFila(
                        c.Estado,
                        c.NombreContacto,
                        c.TelefonoContacto,
                        c.Intentos.Count(),
                        c.Intentos.Max(i => (DateTime?)i.FechaUtc),
                        c.CerradaUtc))
                    .FirstOrDefault()))
            .ToPagedResultAsync(request.Page, request.Size, cancellationToken);

        var items = pagina.Items.Select(f => new BandejaCalidadItemViewModel(
                f.EventoId,
                f.FechaHoraReporteUtc,
                f.FechaHoraCompletadoUtc,
                f.Tipos.Order().ToList(),
                f.Ficha ?? string.Empty,
                f.Denominacion ?? string.Empty,
                f.Tramo,
                f.Evaluacion?.NombreContacto ?? NombrePersona(f.Persona),
                f.Evaluacion?.TelefonoContacto ?? f.Persona?.Telefono,
                f.PersonasConTelefono,
                f.Evaluacion?.Estado,
                f.Evaluacion?.Intentos ?? 0,
                f.Evaluacion?.UltimoIntentoUtc,
                f.Evaluacion?.CerradaUtc))
            .ToList();

        return new PagedResult<BandejaCalidadItemViewModel>(items, pagina.Page, pagina.Size, pagina.TotalCount);
    }

    internal static string? NombrePersona(PersonaFila? persona)
    {
        if (persona is null) return null;
        var nombre = $"{persona.Nombre} {persona.Apellido}".Trim();
        return nombre.Length > 0 ? nombre : persona.Identificacion ?? "Persona no identificada";
    }

    internal sealed record PersonaFila(string? Nombre, string? Apellido, string? Identificacion, string? Telefono);

    private sealed record EvaluacionFila(
        EstadoEvaluacionEnum Estado,
        string? NombreContacto,
        string TelefonoContacto,
        int Intentos,
        DateTime? UltimoIntentoUtc,
        DateTime? CerradaUtc);

    private sealed record Fila(
        int EventoId,
        DateTime FechaHoraReporteUtc,
        DateTime? FechaHoraCompletadoUtc,
        List<string> Tipos,
        string? Ficha,
        string? Denominacion,
        string? Tramo,
        PersonaFila? Persona,
        int PersonasConTelefono,
        EvaluacionFila? Evaluacion);
}
