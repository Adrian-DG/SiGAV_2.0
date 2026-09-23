using Domain.Abstraction;

namespace Domain.Entities.Operaciones;

public class Flota : BaseEntityMetadata, IAuditableMetadata
{
    public required string Numero { get; set; }
    public required string Codigo { get; set; }

    public int DenominacionId  { get; set; }
    public virtual Denominacion? Denominacion { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}