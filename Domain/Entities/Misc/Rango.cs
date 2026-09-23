using Domain.Abstraction;

namespace Domain.Entities.Misc;

public class Rango : NamedMetadata
{
    public required string NombreArmada { get; set; }
}