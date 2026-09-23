using Domain.Abstraction;

namespace Domain.Entities.Operaciones;

public class EventoUnidad : IAuditableMetadata
{
    public int EventoId { get; set; }
    public virtual Evento? Evento { get; set; }
    
    public int NivelDenominacionId { get; set; }
    public virtual NivelDenominacion? NivelDenominacion { get; set; }
    
    public int UnidadId { get; set; }
    public virtual Unidad? Unidad { get; set; }
    
    public int DenominacionId { get; set; }
    public virtual Denominacion? Denominacion { get; set; }
    
    public int AgenteId { get; set; }
    public virtual Agente? Agente { get; set; }

    public bool EsPrincipal { get; set; }

    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}