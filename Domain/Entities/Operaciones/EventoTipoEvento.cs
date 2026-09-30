namespace Domain.Entities.Operaciones;

/// <summary>
/// Tipo de evento atendido en un evento. Parte del agregado: solo se crea o elimina a través de
/// <see cref="Evento.ReemplazarTipos"/>.
/// </summary>
public class EventoTipoEvento
{
    public int EventoId { get; private set; }
    public virtual Evento? Evento { get; private set; }

    public int TipoEventoId { get; private set; }
    public virtual TipoEvento? TipoEvento { get; private set; }

    // Requerido por EF Core
    private EventoTipoEvento() { }

    internal EventoTipoEvento(int tipoEventoId) => TipoEventoId = tipoEventoId;
}
