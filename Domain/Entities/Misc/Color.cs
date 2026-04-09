using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Misc;

[Table("colores", Schema = "misc")]
public class Color : NamedMetadata
{
    
}