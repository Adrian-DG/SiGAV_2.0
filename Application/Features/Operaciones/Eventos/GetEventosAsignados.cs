using Application.Contracts;
using Application.Exceptions;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Eventos;

/// <summary>
/// Evento sin completar de la unidad de la sesión (los que asigna el call center). <see cref="AgenteId"/>:
/// el agente de la unidad que lo atiende (el de la sesión o su componente).
/// </summary>
public record EventoAsignadoViewModel(
    int Id,
    EstadoEventoEnum Estado,
    CanalReporteEnum CanalReporte,
    IReadOnlyList<string> Tipos,
    IReadOnlyList<CategoriaEventoEnum> Categorias,
    decimal Latitud,
    decimal Longitud,
    string? Direccion,
    string Municipio,
    string? Tramo,
    string? Comentario,
    DateTime FechaHoraReporte,
    DateTime? FechaHoraLlegada,
    RolUnidadEventoEnum Rol,
    int AgenteId);

// Query (app): eventos pendientes o en curso de la unidad de la sesión, más antiguos primero.
public record GetEventosAsignadosQuery : IRequest<IReadOnlyList<EventoAsignadoViewModel>>;

public class GetEventosAsignadosQueryHandler(IReadDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetEventosAsignadosQuery, IReadOnlyList<EventoAsignadoViewModel>>
{
    public async Task<IReadOnlyList<EventoAsignadoViewModel>> Handle(GetEventosAsignadosQuery request, CancellationToken cancellationToken)
    {
        var unidadId = EventoAcceso.UnidadDeAlcance(currentUser)
            ?? throw new ForbiddenException("Solo una unidad de la app tiene eventos asignados.");

        var filas = await db.EventoUnidades
            .Where(u => u.UnidadId == unidadId && u.Evento!.IsActive && u.Evento.Estado != EstadoEventoEnum.Completado)
            .OrderBy(u => u.Evento!.FechaHoraReporteUtc)
            .Select(u => new
            {
                u.Evento!.Id,
                u.Evento.Estado,
                u.Evento.CanalReporte,
                Tipos = u.Evento.Tipos.Select(t => new { t.TipoEvento!.Nombre, t.TipoEvento.Categoria }).ToList(),
                u.Evento.Ubicacion.Latitud,
                u.Evento.Ubicacion.Longitud,
                u.Evento.Direccion,
                Municipio = u.Evento.Municipio!.Nombre,
                Tramo = u.Evento.Tramo != null ? u.Evento.Tramo.Nombre : null,
                u.Evento.Comentario,
                u.Evento.FechaHoraReporteUtc,
                u.Evento.FechaHoraLlegadaUtc,
                u.Rol,
                u.AgenteId,
            })
            .ToListAsync(cancellationToken);

        return filas
            .Select(f => new EventoAsignadoViewModel(
                f.Id, f.Estado, f.CanalReporte,
                f.Tipos.Select(t => t.Nombre).OrderBy(n => n).ToList(),
                f.Tipos.Select(t => t.Categoria).Distinct().Order().ToList(),
                f.Latitud, f.Longitud, f.Direccion, f.Municipio, f.Tramo, f.Comentario,
                f.FechaHoraReporteUtc, f.FechaHoraLlegadaUtc, f.Rol, f.AgenteId))
            .ToList();
    }
}
