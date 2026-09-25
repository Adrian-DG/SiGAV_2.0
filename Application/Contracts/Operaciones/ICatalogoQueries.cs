using Application.Features.Catalogos;

namespace Application.Contracts.Operaciones;

public interface ICatalogoQueries
{
    Task<bool> ExisteTramoAsync(int tramoId, CancellationToken cancellationToken = default);
    Task<bool> ExisteNivelDenominacionAsync(int nivelDenominacionId, CancellationToken cancellationToken = default);
    Task<bool> ExisteRangoAsync(int rangoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogoItemViewModel>> ListarAsync(
        CatalogoEnum catalogo, int? provinciaId, int? marcaId, int? tipoVehiculoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TipoEventoItemViewModel>> ListarTiposEventoAsync(CancellationToken cancellationToken = default);

    Task<bool> ExisteMunicipioAsync(int municipioId, CancellationToken cancellationToken = default);

    /// <summary>Devuelve los Ids de la lista que no existen o están inactivos.</summary>
    Task<IReadOnlyList<int>> TiposEventoInexistentesAsync(IReadOnlyCollection<int> tipoEventoIds, CancellationToken cancellationToken = default);

    /// <summary>Devuelve los Ids de la lista que no existen o están inactivos.</summary>
    Task<IReadOnlyList<int>> TramosInexistentesAsync(IReadOnlyCollection<int> tramoIds, CancellationToken cancellationToken = default);

    /// <summary>Devuelve los Ids de la lista que no existen o están inactivos.</summary>
    Task<IReadOnlyList<int>> RegionesAsistenciaInexistentesAsync(IReadOnlyCollection<int> regionIds, CancellationToken cancellationToken = default);
}
