using System.Linq.Expressions;
using Application.Common.Models;
using Application.Contracts.Operaciones;
using Application.Features.Operaciones.Unidades;
using Domain.Entities.Operaciones;
using Domain.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class UnidadQueries(SiGAVContext context) : IUnidadQueries
{
    private static readonly Expression<Func<Unidad, UnidadViewModel>> ToViewModel = u => new UnidadViewModel(
        u.Id,
        u.Ficha,
        u.Placa,
        u.DenominacionId,
        u.Denominacion != null ? u.Denominacion.Nombre : string.Empty,
        u.Denominacion != null ? u.Denominacion.NivelDenominacionId : null,
        u.Denominacion != null ? u.Denominacion.Nivel!.Nombre : string.Empty,
        u.Denominacion != null ? u.Denominacion.TramoId : null,
        u.Denominacion != null ? u.Denominacion.Tramo!.Nombre : string.Empty,
        u.EstaDisponible,
        u.IsActive);

    public async Task<PagedResult<UnidadViewModel>> GetPagedAsync(int page, int size, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var query = context.Unidades.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(u => EF.Functions.Like(u.Ficha, pattern)
                || (u.Placa != null && EF.Functions.Like(u.Placa, pattern))
                || (u.Denominacion != null && EF.Functions.Like(u.Denominacion.Nombre, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.Denominacion != null ? (int)u.Denominacion.Nivel!.Jerarquia : int.MaxValue)
            .ThenBy(u => u.Ficha)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(ToViewModel)
            .ToListAsync(cancellationToken);

        return new PagedResult<UnidadViewModel>(items, page, size, total);
    }

    public async Task<IReadOnlyList<UnidadAutoCompleteViewModel>> AutoCompleteAsync(string term, bool esAmbulancia, int take, CancellationToken cancellationToken = default)
    {
        var pattern = $"%{term}%";

        var query = context.Unidades
            .AsNoTracking()
            .Where(u => u.IsActive)
            .Where(u => EF.Functions.Like(u.Ficha, pattern)
                || (u.Denominacion != null && EF.Functions.Like(u.Denominacion.Nombre, pattern)));

        query = esAmbulancia
            ? query.Where(u => u.Denominacion != null && u.Denominacion.Nivel!.EsAmbulancia)
            : query.Where(u => u.Denominacion == null || !u.Denominacion.Nivel!.EsAmbulancia);

        return await query
            .OrderBy(u => u.Ficha)
            .Take(take)
            .Select(u => new UnidadAutoCompleteViewModel(
                u.Id,
                u.Ficha,
                u.Placa,
                u.Denominacion != null ? u.Denominacion.Nombre : "Sin Denominación",
                u.Denominacion != null ? u.Denominacion.Tramo!.Nombre : "No Disponible",
                u.EstaDisponible))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnidadViewModel>> GetByTramoAsync(int tramoId, CancellationToken cancellationToken = default)
        => await context.Unidades
            .AsNoTracking()
            .Where(u => u.IsActive && u.Denominacion != null && u.Denominacion.TramoId == tramoId)
            .OrderBy(u => u.Ficha)
            .Select(ToViewModel)
            .ToListAsync(cancellationToken);

    public Task<bool> ExisteActivaYDisponibleAsync(string ficha, CancellationToken cancellationToken = default)
        => context.Unidades.AnyAsync(u => u.Ficha == ficha && u.IsActive && u.EstaDisponible, cancellationToken);

    public Task<bool> EstaDisponibleAsync(string ficha, CancellationToken cancellationToken = default)
        => context.Unidades.AnyAsync(u => u.Ficha == ficha && u.EstaDisponible, cancellationToken);

    public Task<NamedViewModel?> GetDenominacionActualAsync(int unidadId, CancellationToken cancellationToken = default)
        => context.Unidades
            .AsNoTracking()
            .Where(u => u.Id == unidadId && u.Denominacion != null)
            .Select(u => new NamedViewModel(u.Denominacion!.Id, u.Denominacion.Nombre))
            .FirstOrDefaultAsync(cancellationToken);
}
