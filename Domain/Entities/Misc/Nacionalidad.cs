using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Misc;

[Table("nacionalidades", Schema = "misc")]
public class Nacionalidad : NamedMetadata
{
    
}