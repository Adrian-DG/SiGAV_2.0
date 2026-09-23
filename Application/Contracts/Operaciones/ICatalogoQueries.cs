namespace Application.Contracts.Operaciones;

public interface ICatalogoQueries
{
    Task<bool> ExisteTramoAsync(int tramoId, CancellationToken cancellationToken = default);
    Task<bool> ExisteTipoUnidadAsync(int tipoUnidadId, CancellationToken cancellationToken = default);
}
