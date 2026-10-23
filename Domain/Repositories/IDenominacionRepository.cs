using Domain.Entities.Operaciones;

namespace Domain.Repositories;

public interface IDenominacionRepository
{
    /// <summary>Denominación activa con su nivel y sus asignaciones de regiones y tramos.</summary>
    Task<Denominacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<NivelDenominacion?> GetNivelAsync(int nivelDenominacionId, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default);
    /// <summary>Denominaciones (activas o no) con alguno de los nombres, sin distinguir mayúsculas.</summary>
    Task<List<Denominacion>> GetByNombresAsync(IReadOnlyCollection<string> nombres, CancellationToken cancellationToken = default);
    void Add(Denominacion denominacion);
}
