using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Abstraction;

public class PersonMetadata : BaseEntityMetadata
{
    [MinLength(8)]
    [MaxLength(11)]
    public required string Identificacion { get; set; }
    
    [MaxLength(50)]
    public required string Nombre { get; set; } 
    
    [MaxLength(50)]
    public required string Apellido { get; set; }
    
    public SexoEnum Sexo { get; set; }
}