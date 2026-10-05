using Domain.Entities.Operaciones;

namespace Domain.Repositories;

public interface IEventoRepository
{
    /// <summary>Evento con sus unidades participantes (necesarias para autorizar y cambiar de estado).</summary>
    Task<Evento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evento con unidades, personas, vehículos y evidencias: lo necesario para agregar una
    /// evidencia (autorizar, validar la persona o el vehículo y detectar reenvíos).
    /// </summary>
    Task<Evento?> GetParaEvidenciasAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Id del evento registrado con esa clave de idempotencia, si existe.</summary>
    Task<int?> GetIdByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    void Add(Evento evento);
}
