using Application.Common;
using Application.Common.Models;
using Application.Contracts;
using Application.Exceptions;
using Domain.Enums;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Eventos;

// Query: listado paginado, más recientes primero. Las fechas son días operativos de RD.
// En la app solo aparecen los eventos en que participa su unidad y nunca los anulados.
public record GetEventosQuery(
    EstadoEventoEnum? Estado = null,
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    bool IncluirAnulados = false,
    int Page = 1,
    int Size = 20) : IRequest<PagedResult<EventoListItemViewModel>>, IPagedQuery;

public class GetEventosQueryValidator : PagedQueryValidator<GetEventosQuery>
{
    public GetEventosQueryValidator()
    {
        RuleFor(x => x.Estado).IsInEnum().When(x => x.Estado.HasValue).WithMessage("El estado no es válido.");
        RuleFor(x => x.Desde)
            .Must((q, desde) => desde is null || q.Hasta is null || desde <= q.Hasta)
            .WithMessage("La fecha inicial no puede ser posterior a la final.");
    }
}

public class GetEventosQueryHandler(IReadDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetEventosQuery, PagedResult<EventoListItemViewModel>>
{
    public async Task<PagedResult<EventoListItemViewModel>> Handle(GetEventosQuery request, CancellationToken cancellationToken)
    {
        var unidadId = EventoAcceso.UnidadDeAlcance(currentUser);
        // Los anulados solo los ve front desk, y solo si los pide
        var incluirAnulados = unidadId is null && request.IncluirAnulados;

        var query = db.Eventos;
        if (!incluirAnulados) query = query.Where(e => e.IsActive);
        if (request.Estado is { } estado) query = query.Where(e => e.Estado == estado);
        if (request.Desde is { } desde)
        {
            var desdeUtc = ZonaHorariaOperativa.InicioDelDiaUtc(desde);
            query = query.Where(e => e.FechaHoraReporteUtc >= desdeUtc);
        }
        if (request.Hasta is { } hasta)
        {
            var hastaExclusivoUtc = ZonaHorariaOperativa.InicioDelDiaUtc(hasta.AddDays(1));
            query = query.Where(e => e.FechaHoraReporteUtc < hastaExclusivoUtc);
        }
        if (unidadId is { } id) query = query.Where(e => e.Unidades.Any(u => u.UnidadId == id));

        var pagina = await query
            .OrderByDescending(e => e.FechaHoraReporteUtc)
            .ThenByDescending(e => e.Id)
            .Select(e => new FilaEvento(
                e.Id,
                e.Estado,
                e.Direccion,
                e.FechaHoraReporteUtc,
                e.Tipos.Select(t => new TipoFila(t.TipoEvento!.Nombre, t.TipoEvento.Categoria)).ToList(),
                e.Unidades
                    .Where(u => u.Rol == RolUnidadEventoEnum.Principal)
                    .Select(u => new PrincipalFila(u.Unidad!.Ficha, u.Denominacion!.Nombre))
                    .FirstOrDefault(),
                e.Ciudadanos.OrderBy(c => c.Id).Select(c => c.Persona).FirstOrDefault(),
                e.Ciudadanos.Where(c => c.Vehiculo != null).OrderBy(c => c.Id).Select(c => c.Vehiculo).FirstOrDefault()))
            .ToPagedResultAsync(request.Page, request.Size, cancellationToken);

        var catalogo = await CatalogoVehiculo.CargarAsync(db, pagina.Items.Select(f => f.Vehiculo), cancellationToken);

        var items = pagina.Items.Select(f => new EventoListItemViewModel(
                f.Id,
                f.Estado,
                f.Tipos.Select(t => t.Nombre).OrderBy(n => n).ToList(),
                f.Tipos.Select(t => t.Categoria).Distinct().Order().ToList(),
                NombrePersona(f.Persona),
                f.Vehiculo is null ? null : catalogo.Describir(f.Vehiculo).Descripcion,
                f.Direccion,
                f.FechaHoraReporteUtc,
                f.Principal?.Ficha ?? string.Empty,
                f.Principal?.Denominacion ?? string.Empty))
            .ToList();

        return new PagedResult<EventoListItemViewModel>(items, pagina.Page, pagina.Size, pagina.TotalCount);
    }

    private static string? NombrePersona(DatosPersona? persona)
    {
        if (persona is null) return null;
        var nombre = $"{persona.Nombre} {persona.Apellido}".Trim();
        return nombre.Length > 0 ? nombre : persona.Identificacion ?? "Persona no identificada";
    }

    private sealed record TipoFila(string Nombre, CategoriaEventoEnum Categoria);
    private sealed record PrincipalFila(string Ficha, string Denominacion);
    private sealed record FilaEvento(
        int Id,
        EstadoEventoEnum Estado,
        string? Direccion,
        DateTime FechaHoraReporteUtc,
        List<TipoFila> Tipos,
        PrincipalFila? Principal,
        DatosPersona? Persona,
        DatosVehiculo? Vehiculo);
}

// Query: detalle. Para la app, un evento de otra unidad responde 404 (no se revela que existe).
public record GetEventoQuery(int EventoId) : IRequest<EventoDetalleViewModel>;

public class GetEventoQueryHandler(IReadDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetEventoQuery, EventoDetalleViewModel>
{
    public async Task<EventoDetalleViewModel> Handle(GetEventoQuery request, CancellationToken cancellationToken)
    {
        var query = db.Eventos.Where(e => e.Id == request.EventoId);
        if (EventoAcceso.UnidadDeAlcance(currentUser) is { } unidadId)
            query = query.Where(e => e.Unidades.Any(u => u.UnidadId == unidadId));

        var evento = await query
            .Include(e => e.Municipio!).ThenInclude(m => m.Provincia)
            .Include(e => e.Tramo)
            .Include(e => e.Tipos).ThenInclude(t => t.TipoEvento)
            .Include(e => e.Unidades).ThenInclude(u => u.Unidad)
            .Include(e => e.Unidades).ThenInclude(u => u.Denominacion)
            .Include(e => e.Unidades).ThenInclude(u => u.NivelDenominacion)
            .Include(e => e.Unidades).ThenInclude(u => u.Agente!).ThenInclude(a => a.Rango)
            .Include(e => e.Ciudadanos)
            .Include(e => e.Evidencias)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);

        var catalogo = await CatalogoVehiculo.CargarAsync(db, evento.Ciudadanos.Select(c => c.Vehiculo), cancellationToken);
        var nacionalidadIds = evento.Ciudadanos.Select(c => c.Persona.NacionalidadId).OfType<int>().Distinct().ToList();
        var nacionalidades = nacionalidadIds.Count == 0
            ? []
            : await db.Nacionalidades
                .Where(n => nacionalidadIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Nombre, cancellationToken);

        return new EventoDetalleViewModel(
            evento.Id,
            evento.RequestId,
            evento.Estado,
            evento.CanalReporte,
            evento.TipoCierre,
            evento.IsActive,
            evento.Ubicacion.Latitud,
            evento.Ubicacion.Longitud,
            evento.Direccion,
            evento.MunicipioId,
            evento.Municipio?.Nombre ?? string.Empty,
            evento.Municipio?.Provincia?.Nombre ?? string.Empty,
            evento.TramoId,
            evento.Tramo?.Nombre,
            evento.Comentario,
            evento.FechaHoraReporteUtc,
            evento.FechaHoraLlegadaUtc,
            evento.FechaHoraCompletadoUtc,
            evento.Tipos
                .Select(t => new EventoTipoViewModel(t.TipoEventoId, t.TipoEvento?.Nombre ?? string.Empty, t.TipoEvento?.Categoria ?? default))
                .OrderBy(t => t.Nombre)
                .ToList(),
            evento.Unidades
                .OrderBy(u => u.Rol)
                .Select(u => new EventoUnidadViewModel(
                    u.UnidadId,
                    u.Unidad?.Ficha ?? string.Empty,
                    u.DenominacionId,
                    u.Denominacion?.Nombre ?? string.Empty,
                    u.NivelDenominacion?.Nombre ?? string.Empty,
                    u.Rol,
                    u.AgenteId,
                    u.Agente?.GetInfo ?? string.Empty))
                .ToList(),
            evento.Ciudadanos
                .OrderBy(c => c.Id)
                .Select(c => new EventoCiudadanoViewModel(
                    c.Id,
                    c.Rol,
                    c.Persona.Identificacion,
                    c.Persona.Nombre,
                    c.Persona.Apellido,
                    c.Persona.Sexo,
                    c.Persona.Telefono,
                    c.Persona.NacionalidadId is { } n ? nacionalidades.GetValueOrDefault(n) : null,
                    c.Vehiculo is null ? null : catalogo.Describir(c.Vehiculo)))
                .ToList(),
            evento.Evidencias
                .OrderBy(e => e.Id)
                .Select(e => new EventoEvidenciaViewModel(e.Id, e.Tipo, e.Ubicacion, e.ContentType, e.RegistradaUtc))
                .ToList(),
            evento.CreatedAt);
    }
}
