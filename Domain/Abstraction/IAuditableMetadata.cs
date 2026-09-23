namespace Domain.Abstraction;

/// <summary>Auditoría técnica de la fila, en UTC (cuándo y quién la guardó o modificó).</summary>
public interface IAuditableMetadata
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}