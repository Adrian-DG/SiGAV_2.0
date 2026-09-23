using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Tipos de evento atendidos en un evento (un evento puede tener varios, como en SiGAV 1.0).
/// </summary>
[Table("evento_tipo_evento", Schema = "operaciones")]
public class EventoTipoEvento
{
    [ForeignKey(nameof(Evento))]
    public int EventoId { get; set; }
    public virtual Evento? Evento { get; set; }

    [ForeignKey(nameof(TipoEvento))]
    public int TipoEventoId { get; set; }
    public virtual TipoEvento? TipoEvento { get; set; }
}
