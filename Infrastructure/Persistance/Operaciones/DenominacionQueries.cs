using Application.Common.Models;
using Application.Contracts.Operaciones;
using Application.Features.Operaciones.Denominaciones;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class DenominacionQueries(SiGAVContext context) : IDenominacionQueries
{
    public async Task<PagedResult<DenominacionViewModel>> GetPagedAsync(int page, int size, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var query = context.Denominaciones.AsNoTracking().Where(d => d.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(d => EF.Functions.Like(d.Nombre, pattern));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(d => d.Nivel!.Jerarquia)
            .ThenBy(d => d.Nombre)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(d => new DenominacionViewModel(
                d.Id,
                d.Nombre,
                d.NivelDenominacionId,
                d.Nivel!.Nombre,
                d.Nivel.Jerarquia,
                d.TramoId,
                d.Tramo!.Nombre))
            .ToListAsync(cancellationToken);

        return new PagedResult<DenominacionViewModel>(items, page, size, total);
    }

    public async Task<DenominacionDetalleViewModel?> GetDetalleAsync(int denominacionId, CancellationToken cancellationToken = default)
    {
        var detalle = await context.Denominaciones
            .AsNoTracking()
            .Where(d => d.Id == denominacionId && d.IsActive)
            .Select(d => new
            {
                d.Id,
                d.Nombre,
                d.NivelDenominacionId,
                Nivel = d.Nivel!.Nombre,
                d.Nivel.Jerarquia,
                d.TramoId,
                Tramo = d.Tramo!.Nombre,
                Macros = d.Regiones.Where(r => r.RegionMacro != null).Select(r => r.RegionMacro!.Value).ToList(),
                Regiones = d.Regiones
                    .Where(r => r.RegionAsistenciaId != null)
                    .Select(r => new AsignacionViewModel(r.RegionAsistencia!.Id, r.RegionAsistencia.Nombre))
                    .ToList(),
                Tramos = d.Tramos.Select(t => new AsignacionViewModel(t.Tramo!.Id, t.Tramo.Nombre)).ToList()
            })
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        return detalle is null
            ? null
            : new DenominacionDetalleViewModel(
                detalle.Id,
                detalle.Nombre,
                detalle.NivelDenominacionId,
                detalle.Nivel,
                detalle.Jerarquia,
                detalle.TramoId,
                detalle.Tramo,
                detalle.Macros.Order().ToList(),
                detalle.Regiones.OrderBy(r => r.Nombre).ToList(),
                detalle.Tramos.OrderBy(t => t.Nombre).ToList());
    }
}
