using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;
using Domain.Entities.Historico;
using Domain.Entities.Misc;

namespace Domain.Entities.Operaciones;

[Table("evento_ciudadano", Schema = "operaciones")]
public class EventoCiudadano : IAuditableMetadata
{
    [ForeignKey(nameof(Evento))]
    public int EventoId { get; set; }
    public virtual Evento? Evento { get; set; }
    
    [ForeignKey(nameof(Ciudadano))]
    public int CiudadanoId { get; set; }
    public virtual Ciudadano? Ciudadano { get; set; }

    [ForeignKey(nameof(Vehiculo))] 
    public int VehiculoId { get; set; }
    public virtual Vehiculo? Vehiculo { get; set; }
    
    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}