using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Misc;

[Table("marcas", Schema = "misc")]
public class Marca : NamedMetadata
{
    
}