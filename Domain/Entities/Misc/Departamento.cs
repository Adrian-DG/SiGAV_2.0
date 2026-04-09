using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Misc;

[Table("departamentos", Schema = "misc")]
public class Departamento : NamedMetadata
{
    
}