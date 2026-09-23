using System.Linq.Expressions;
using Application.Common.Models;
using Application.Contracts.Operaciones;
using Application.Features.Operaciones.Agentes;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class AgenteQueries(SiGAVContext context) : IAgenteQueries
{
    // La Armada (ARD) usa su propia nomenclatura de rangos (misma regla que Agente.GetRango)
    private static readonly Expression<Func<Agente, AgenteViewModel>> ToViewModel = a => new AgenteViewModel(
        a.Id,
        a.Identificacion,
        a.Nombre,
        a.Apellido,
        a.Apellido + " " + a.Nombre,
        a.Sexo,
        a.Institucion,
        a.RangoId,
        a.Institucion == InstitucionEnum.ARD ? a.Rango!.NombreArmada : a.Rango!.Nombre,
        a.AreaOperativa,
        a.AccesoTotal,
        a.Especialidad,
        a.Autorizado,
        a.IsActive,
        a.CreatedAt);

    public async Task<PagedResult<AgenteViewModel>> GetPagedAsync(
        int page,
        int size,
        string? searchTerm,
        bool? autorizado,
        AreaOperativaEnum? areaOperativa,
        bool incluirInactivos,
        CancellationToken cancellationToken = default)
    {
        var query = context.Agentes.AsNoTracking();

        if (!incluirInactivos) query = query.Where(a => a.IsActive);
        if (autorizado.HasValue) query = query.Where(a => a.Autorizado == autorizado.Value);
        if (areaOperativa.HasValue) query = query.Where(a => a.AreaOperativa == areaOperativa.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(a => EF.Functions.Like(a.Identificacion, pattern)
                || EF.Functions.Like(a.Nombre, pattern)
                || EF.Functions.Like(a.Apellido, pattern));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(a => a.Apellido)
            .ThenBy(a => a.Nombre)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(ToViewModel)
            .ToListAsync(cancellationToken);

        return new PagedResult<AgenteViewModel>(items, page, size, total);
    }

    public Task<AgenteViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => context.Agentes.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(ToViewModel)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ConfirmAgenteViewModel> ConfirmarAsync(string identificacion, CancellationToken cancellationToken = default)
    {
        var estado = await context.Agentes.AsNoTracking()
            .Where(a => a.Identificacion == identificacion)
            .Select(a => new { a.IsActive, a.Autorizado })
            .FirstOrDefaultAsync(cancellationToken);

        return new ConfirmAgenteViewModel(estado is not null, estado is { IsActive: true, Autorizado: true });
    }

    public async Task<IReadOnlyList<AgenteAutoCompleteViewModel>> AutoCompleteAsync(string term, AreaOperativaEnum? areaOperativa, int take, CancellationToken cancellationToken = default)
    {
        var pattern = $"%{term}%";

        var query = context.Agentes.AsNoTracking()
            .Where(a => a.IsActive && a.Autorizado)
            .Where(a => EF.Functions.Like(a.Identificacion, pattern)
                || EF.Functions.Like(a.Nombre, pattern)
                || EF.Functions.Like(a.Apellido, pattern));

        if (areaOperativa.HasValue) query = query.Where(a => a.AreaOperativa == areaOperativa.Value);

        return await query
            .OrderBy(a => a.Apellido)
            .ThenBy(a => a.Nombre)
            .Take(take)
            .Select(a => new AgenteAutoCompleteViewModel(
                a.Id,
                a.Identificacion,
                (a.Institucion == InstitucionEnum.ARD ? a.Rango!.NombreArmada : a.Rango!.Nombre) + ", " + a.Apellido + " " + a.Nombre,
                a.AreaOperativa))
            .ToListAsync(cancellationToken);
    }
}
