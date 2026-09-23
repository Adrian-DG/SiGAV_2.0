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
            .OrderBy(d => d.Nombre)
            .ThenBy(d => d.TipoUnidadId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(d => new DenominacionViewModel(
                d.Id,
                d.Nombre,
                d.TipoUnidadId,
                d.TipoUnidad!.Nombre,
                d.TramoId,
                d.Tramo!.Nombre))
            .ToListAsync(cancellationToken);

        return new PagedResult<DenominacionViewModel>(items, page, size, total);
    }
}
