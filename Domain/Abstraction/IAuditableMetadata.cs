namespace Domain.Abstraction;

public interface IAuditableMetadata
{
    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}