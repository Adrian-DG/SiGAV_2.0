using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Misc;

[Table("rangos", Schema = "misc")]
public class Rango : NamedMetadata
{
    public required string NombreArmada { get; set; }
}