using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Operaciones;

[Table("flotas", Schema = "operaciones")]
public class Flota : BaseEntityMetadata, IAuditableMetadata
{
    public required string Numero { get; set; }
    public required string Codigo { get; set; }

    [ForeignKey(nameof(Denominacion))]
    public int DenominacionId  { get; set; }
    public virtual Denominacion? Denominacion { get; set; }
    
    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}