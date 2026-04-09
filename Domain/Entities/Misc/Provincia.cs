using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Misc;

[Table("provincias", Schema = "misc")]
public class Provincia : NamedMetadata
{
    
}