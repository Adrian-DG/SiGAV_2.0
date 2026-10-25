using Domain.Entities.Operaciones;

namespace Domain.Repositories;

public interface IAgenteRepository
{
    Task<Agente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Agente activo, sin seguimiento de cambios y con su rango (para el login).</summary>
    Task<Agente?> GetActivoByIdentificacionAsync(string identificacion, CancellationToken cancellationToken = default);

    Task<bool> ExisteIdentificacionAsync(string identificacion, int? excluirAgenteId = null, CancellationToken cancellationToken = default);
    /// <summary>Agentes (activos o no) cuyas cédulas están en la lista, con seguimiento de cambios.</summary>
    Task<List<Agente>> GetByIdentificacionesAsync(IReadOnlyCollection<string> identificaciones, CancellationToken cancellationToken = default);
    void Add(Agente agente);
}
