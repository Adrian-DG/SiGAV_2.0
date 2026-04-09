using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Operaciones;

[Table("denominaciones", Schema = "operaciones")]
public class Denominacion : NamedMetadata, IAuditableMetadata
{
    [ForeignKey(nameof(Tramo))]
    public int TramoId { get; set; }
    public virtual Tramo? Tramo { get; set; }
    
    [ForeignKey(nameof(TipoUnidad))] 
    public int TipoUnidadId { get; set; }
    public virtual TipoUnidad? TipoUnidad { get; set; }
    
    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}