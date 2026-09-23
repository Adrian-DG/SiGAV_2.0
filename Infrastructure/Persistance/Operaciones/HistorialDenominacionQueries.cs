using Application.Common.Models;
using Application.Contracts.Operaciones;
using Application.Features.Operaciones.Historial;
using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class HistorialDenominacionQueries(SiGAVContext context) : IHistorialDenominacionQueries
{
    public Task<PagedResult<HistorialDenominacionViewModel>> GetByUnidadAsync(int unidadId, int page, int size, CancellationToken cancellationToken = default)
        => GetPagedAsync(context.HistorialDenominaciones.Where(h => h.UnidadId == unidadId), page, size, cancellationToken);

    public Task<PagedResult<HistorialDenominacionViewModel>> GetByDenominacionAsync(int denominacionId, int page, int size, CancellationToken cancellationToken = default)
        => GetPagedAsync(
            context.HistorialDenominaciones.Where(h => h.DenominacionAnteriorId == denominacionId || h.DenominacionNuevaId == denominacionId),
            page, size, cancellationToken);

    private async Task<PagedResult<HistorialDenominacionViewModel>> GetPagedAsync(
        IQueryable<HistorialDenominacionUnidad> query,
        int page,
        int size,
        CancellationToken cancellationToken)
    {
        query = query.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var items = await (
                from h in query
                join u in context.Unidades on h.UnidadId equals u.Id
                join usr in context.Users on h.UsuarioId equals usr.Id into usuarios
                from usr in usuarios.DefaultIfEmpty()
                orderby h.FechaUtc descending, h.Id descending
                select new HistorialDenominacionViewModel(
                    h.Id,
                    h.FechaUtc,
                    h.TipoCambio,
                    h.UnidadId,
                    u.Ficha,
                    h.DenominacionAnteriorId,
                    h.DenominacionAnterior != null ? h.DenominacionAnterior.Nombre : null,
                    h.DenominacionNuevaId,
                    h.DenominacionNueva != null ? h.DenominacionNueva.Nombre : null,
                    h.UnidadRelacionadaId,
                    h.UnidadRelacionada != null ? h.UnidadRelacionada.Ficha : null,
                    h.UsuarioId,
                    usr != null ? usr.UserName : null,
                    h.Observacion))
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        // SQLite no conserva DateTimeKind: se marca como UTC para que se serialice con "Z"
        items = items.Select(i => i with { FechaUtc = DateTime.SpecifyKind(i.FechaUtc, DateTimeKind.Utc) }).ToList();

        return new PagedResult<HistorialDenominacionViewModel>(items, page, size, total);
    }
}
