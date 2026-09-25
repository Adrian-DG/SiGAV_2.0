using Application.Contracts.Operaciones;
using Domain.Enums;
using MediatR;

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

public class GetCatalogoQueryHandler(ICatalogoQueries catalogos) : IRequestHandler<GetCatalogoQuery, IReadOnlyList<CatalogoItemViewModel>>
{
    public Task<IReadOnlyList<CatalogoItemViewModel>> Handle(GetCatalogoQuery request, CancellationToken cancellationToken)
        => catalogos.ListarAsync(request.Catalogo, request.ProvinciaId, request.MarcaId, request.TipoVehiculoId, cancellationToken);
}

public record GetTiposEventoQuery : IRequest<IReadOnlyList<TipoEventoItemViewModel>>;

public class GetTiposEventoQueryHandler(ICatalogoQueries catalogos) : IRequestHandler<GetTiposEventoQuery, IReadOnlyList<TipoEventoItemViewModel>>
{
    public Task<IReadOnlyList<TipoEventoItemViewModel>> Handle(GetTiposEventoQuery request, CancellationToken cancellationToken)
        => catalogos.ListarTiposEventoAsync(cancellationToken);
}
