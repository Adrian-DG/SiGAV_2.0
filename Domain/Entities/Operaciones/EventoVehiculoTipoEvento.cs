namespace Domain.Entities.Operaciones;

/// <summary>
/// Tipo de evento atendido a un vehículo (p. ej. Neumático, Calentamiento). Parte del agregado:
/// se crea a través de <see cref="Evento.AgregarVehiculo"/> y siempre es uno de los tipos del evento.
/// </summary>
public class EventoVehiculoTipoEvento
{
    public int EventoVehiculoId { get; private set; }
    public virtual EventoVehiculoInfo? EventoVehiculo { get; private set; }

    public int TipoEventoId { get; private set; }
    public virtual TipoEvento? TipoEvento { get; private set; }

    // Requerido por EF Core
    private EventoVehiculoTipoEvento() { }

    internal EventoVehiculoTipoEvento(int tipoEventoId) => TipoEventoId = tipoEventoId;
}
