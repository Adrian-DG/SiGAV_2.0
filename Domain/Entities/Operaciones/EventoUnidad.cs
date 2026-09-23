using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Operaciones;

[Table("evento_unidad", Schema = "operaciones")]
public class EventoUnidad : IAuditableMetadata
{
    [ForeignKey(nameof(Evento))]
    public int EventoId { get; set; }
    public virtual Evento? Evento { get; set; }
    
    [ForeignKey(nameof(NivelDenominacion))]
    public int NivelDenominacionId { get; set; }
    public virtual NivelDenominacion? NivelDenominacion { get; set; }
    
    [ForeignKey(nameof(Unidad))]
    public int UnidadId { get; set; }
    public virtual Unidad? Unidad { get; set; }
    
    [ForeignKey(nameof(Denominacion))] 
    public int DenominacionId { get; set; }
    public virtual Denominacion? Denominacion { get; set; }
    
    [ForeignKey(nameof(Agente))]
    public int AgenteId { get; set; }
    public virtual Agente? Agente { get; set; }

    [DefaultValue(true)]
    public bool EsPrincipal { get; set; }

    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}