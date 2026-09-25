using Application.Contracts;
using Domain.Abstraction;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalogos;

public record CatalogoItemViewModel(int Id, string Nombre);

public record TipoEventoItemViewModel(int Id, string Nombre, CategoriaEventoEnum Categoria);

public enum CatalogoEnum
{
    Provincias,
    Municipios,
    TiposVehiculo,
    Marcas,
    Modelos,
    Colores,
    Nacionalidades
}

/// <summary>
/// Catálogo activo, ordenado por nombre. Municipios se filtra por ProvinciaId; Modelos por MarcaId
/// (y opcionalmente TipoVehiculoId).
/// </summary>
public record GetCatalogoQuery(CatalogoEnum Catalogo, int? ProvinciaId = null, int? MarcaId = null, int? TipoVehiculoId = null)
    : IRequest<IReadOnlyList<CatalogoItemViewModel>>;

public class GetCatalogoQueryHandler(IReadDbContext db) : IRequestHandler<GetCatalogoQuery, IReadOnlyList<CatalogoItemViewModel>>
{
    public async Task<IReadOnlyList<CatalogoItemViewModel>> Handle(GetCatalogoQuery request, CancellationToken cancellationToken)
    {
        IQueryable<NamedMetadata> query = request.Catalogo switch
        {
            CatalogoEnum.Provincias => db.Provincias,
            // Sin provincia no se listan todos los municipios del país
            CatalogoEnum.Municipios => db.Municipios.Where(m => m.ProvinciaId == (request.ProvinciaId ?? 0)),
            CatalogoEnum.TiposVehiculo => db.TiposVehiculo,
            CatalogoEnum.Marcas => db.Marcas,
            CatalogoEnum.Modelos => db.Modelos.Where(m => m.MarcaId == (request.MarcaId ?? 0)
                && (request.TipoVehiculoId == null || m.TipoVehiculoId == request.TipoVehiculoId)),
            CatalogoEnum.Colores => db.Colores,
            CatalogoEnum.Nacionalidades => db.Nacionalidades,
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Catalogo, "Catálogo no soportado.")
        };

        return await query
            .Where(x => x.IsActive)
            .OrderBy(x => x.Nombre)
            .Select(x => new CatalogoItemViewModel(x.Id, x.Nombre))
            .ToListAsync(cancellationToken);
    }
}

public record GetTiposEventoQuery : IRequest<IReadOnlyList<TipoEventoItemViewModel>>;

public class GetTiposEventoQueryHandler(IReadDbContext db) : IRequestHandler<GetTiposEventoQuery, IReadOnlyList<TipoEventoItemViewModel>>
{
    public async Task<IReadOnlyList<TipoEventoItemViewModel>> Handle(GetTiposEventoQuery request, CancellationToken cancellationToken)
        => await db.TiposEvento
            .Where(t => t.IsActive)
            .OrderBy(t => t.Categoria).ThenBy(t => t.Nombre)
            .Select(t => new TipoEventoItemViewModel(t.Id, t.Nombre, t.Categoria))
            .ToListAsync(cancellationToken);
}
