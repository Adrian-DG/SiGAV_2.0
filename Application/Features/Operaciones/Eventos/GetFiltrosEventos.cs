using Application.Contracts;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Eventos;

public record OpcionFiltroViewModel(int Id, string Nombre, string? Detalle = null);

public record FiltrosEventosViewModel(
    IReadOnlyList<OpcionFiltroViewModel> Agentes,
    IReadOnlyList<OpcionFiltroViewModel> Unidades,
    IReadOnlyList<OpcionFiltroViewModel> Denominaciones,
    IReadOnlyList<OpcionFiltroViewModel> Tramos);

// Query: opciones de los filtros del listado de eventos (front desk). Agentes y unidades: solo los
// que han participado en algún evento (los demás no darían resultados). Denominaciones y tramos:
// los activos más los inactivos que aparecen en eventos.
public record GetFiltrosEventosQuery : IRequest<FiltrosEventosViewModel>;

public class GetFiltrosEventosQueryHandler(IReadDbContext db) : IRequestHandler<GetFiltrosEventosQuery, FiltrosEventosViewModel>
{
    public async Task<FiltrosEventosViewModel> Handle(GetFiltrosEventosQuery request, CancellationToken cancellationToken)
    {
        var agentes = await db.Agentes
            .Where(a => db.EventoUnidades.Any(eu => eu.AgenteId == a.Id))
            .OrderBy(a => a.Apellido).ThenBy(a => a.Nombre)
            .Select(a => new
            {
                a.Id,
                a.Nombre,
                a.Apellido,
                a.Identificacion,
                a.Institucion,
                Rango = a.Institucion == InstitucionEnum.ARD ? a.Rango!.NombreArmada : a.Rango!.Nombre
            })
            .ToListAsync(cancellationToken);

        var unidades = await db.Unidades
            .Where(u => db.EventoUnidades.Any(eu => eu.UnidadId == u.Id))
            .OrderBy(u => u.Ficha)
            .Select(u => new OpcionFiltroViewModel(u.Id, u.Ficha, u.Placa))
            .ToListAsync(cancellationToken);

        var denominaciones = await db.Denominaciones
            .Where(d => d.IsActive || db.EventoUnidades.Any(eu => eu.DenominacionId == d.Id))
            .OrderBy(d => d.Nombre)
            .Select(d => new OpcionFiltroViewModel(d.Id, d.Nombre, d.IsActive ? null : "Inactiva"))
            .ToListAsync(cancellationToken);

        var tramos = await db.Tramos
            .Where(t => t.IsActive || db.Eventos.Any(e => e.TramoId == t.Id))
            .OrderBy(t => t.Nombre)
            .Select(t => new OpcionFiltroViewModel(t.Id, t.Nombre, t.IsActive ? null : "Inactivo"))
            .ToListAsync(cancellationToken);

        return new FiltrosEventosViewModel(
            agentes
                .Select(a => new OpcionFiltroViewModel(
                    a.Id,
                    $"{a.Nombre} {a.Apellido}",
                    $"{a.Rango} · {a.Institucion} · {a.Identificacion}"))
                .ToList(),
            unidades,
            denominaciones,
            tramos);
    }
}
