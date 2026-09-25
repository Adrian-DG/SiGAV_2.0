using Application.Common.Models;
using Domain.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace Application.Common;

public static class QueryableExtensions
{
    /// <summary>Cuenta el total y trae la página pedida. La consulta ya debe venir ordenada.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int size, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, page, size, total);
    }

    /// <summary>Patrón "contiene" para LIKE, o null si no hay término de búsqueda.</summary>
    public static string? PatronBusqueda(string? termino)
        => string.IsNullOrWhiteSpace(termino) ? null : $"%{termino.Trim()}%";

    public static Task<bool> ExisteActivoAsync<T>(this IQueryable<T> query, int id, CancellationToken cancellationToken)
        where T : BaseEntityMetadata
        => query.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);

    /// <summary>true si todos los Ids existen y están activos (una lista vacía o null cumple).</summary>
    public static async Task<bool> ExistenActivosAsync<T>(this IQueryable<T> query, IEnumerable<int>? ids, CancellationToken cancellationToken)
        where T : BaseEntityMetadata
    {
        var distintos = ids?.Distinct().ToList() ?? [];
        if (distintos.Count == 0) return true;

        var existentes = await query.CountAsync(x => distintos.Contains(x.Id) && x.IsActive, cancellationToken);
        return existentes == distintos.Count;
    }
}
