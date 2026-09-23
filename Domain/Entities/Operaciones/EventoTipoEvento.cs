namespace Domain.Entities.Operaciones;

/// <summary>
/// Tipos de evento atendidos en un evento (un evento puede tener varios, como en SiGAV 1.0).
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
