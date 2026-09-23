namespace Domain.Entities.Operaciones;

/// <summary>
/// Tipos de evento atendidos en un evento (un evento puede tener varios, como en SiGAV 1.0).
/// </summary>
public class EventoTipoEvento
{
    public int EventoId { get; set; }
    public virtual Evento? Evento { get; set; }

    public int TipoEventoId { get; set; }
    public virtual TipoEvento? TipoEvento { get; set; }
}
