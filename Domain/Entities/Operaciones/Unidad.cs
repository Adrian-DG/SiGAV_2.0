using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Operaciones;

[Table("unidades", Schema = "operaciones")]
public class Unidad : BaseEntityMetadata, IAuditableMetadata
{
    public required string Ficha { get; set; }
    public string? Placa { get; set; }
    
    [ForeignKey(nameof(Denominacion))]
    public int DenominacionId { get; set; }
    public virtual Denominacion? Denominacion { get; set; }
    
    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}