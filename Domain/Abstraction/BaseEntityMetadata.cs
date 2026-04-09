using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Abstraction;

public class BaseEntityMetadata
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    
    [DefaultValue(true)]
    public bool IsActive { get; set; }
}