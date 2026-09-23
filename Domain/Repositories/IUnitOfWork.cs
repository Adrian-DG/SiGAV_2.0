namespace Domain.Repositories;

/// <summary>
/// Confirma de forma atómica los cambios hechos sobre los agregados en una operación.
/// A diferencia del UnitOfWork de SiGAV 1.0, no expone repositorios: estos se inyectan directamente.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
