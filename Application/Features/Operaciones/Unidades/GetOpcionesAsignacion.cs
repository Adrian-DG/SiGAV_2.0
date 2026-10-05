using Application.Contracts;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Unidades;

public record UnidadOpcionViewModel(int Id, string Ficha, string? Placa, int? DenominacionId, string? Denominacion, bool EstaDisponible);

/// <param name="UnidadId">Unidad activa que la usa ahora (null: libre); al asignarla a otra, esa unidad la pierde.</param>
public record DenominacionOpcionViewModel(
    int Id,
    string Nombre,
    string NivelDenominacion,
    JerarquiaEnum Jerarquia,
    string Tramo,
    int? UnidadId,
    string? UnidadFicha);

public record NivelOpcionViewModel(int Id, string Nombre, JerarquiaEnum Jerarquia);

public record TramoOpcionViewModel(int Id, string Nombre);

public record OpcionesAsignacionViewModel(
    IReadOnlyList<UnidadOpcionViewModel> Unidades,
    IReadOnlyList<DenominacionOpcionViewModel> Denominaciones,
    IReadOnlyList<TramoOpcionViewModel> Tramos,
    IReadOnlyList<NivelOpcionViewModel> Niveles);

// Query: todo lo que necesitan los formularios de asignación y alta del front desk (solo activos)
public record GetOpcionesAsignacionQuery : IRequest<OpcionesAsignacionViewModel>;

public class GetOpcionesAsignacionQueryHandler(IReadDbContext db) : IRequestHandler<GetOpcionesAsignacionQuery, OpcionesAsignacionViewModel>
{
    public async Task<OpcionesAsignacionViewModel> Handle(GetOpcionesAsignacionQuery request, CancellationToken cancellationToken)
    {
        var unidades = await db.Unidades
            .Where(u => u.IsActive)
            .OrderBy(u => u.Ficha)
            .Select(u => new UnidadOpcionViewModel(
                u.Id,
                u.Ficha,
                u.Placa,
                u.DenominacionId,
                u.Denominacion != null ? u.Denominacion.Nombre : null,
                u.EstaDisponible))
            .ToListAsync(cancellationToken);

        var denominaciones = await db.Denominaciones
            .Where(d => d.IsActive)
            .OrderBy(d => d.Nombre)
            .Select(d => new DenominacionOpcionViewModel(
                d.Id,
                d.Nombre,
                d.Nivel!.Nombre,
                d.Nivel.Jerarquia,
                d.Tramo!.Nombre,
                db.Unidades.Where(u => u.IsActive && u.DenominacionId == d.Id).Select(u => (int?)u.Id).FirstOrDefault(),
                db.Unidades.Where(u => u.IsActive && u.DenominacionId == d.Id).Select(u => u.Ficha).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var tramos = await db.Tramos
            .Where(t => t.IsActive)
            .OrderBy(t => t.Nombre)
            .Select(t => new TramoOpcionViewModel(t.Id, t.Nombre))
            .ToListAsync(cancellationToken);

        var niveles = await db.NivelesDenominacion
            .Where(n => n.IsActive)
            .OrderBy(n => n.Jerarquia).ThenBy(n => n.Nombre)
            .Select(n => new NivelOpcionViewModel(n.Id, n.Nombre, n.Jerarquia))
            .ToListAsync(cancellationToken);

        return new OpcionesAsignacionViewModel(unidades, denominaciones, tramos, niveles);
    }
}
