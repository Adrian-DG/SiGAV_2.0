using Domain.Abstraction;

namespace Domain.Entities.Misc;

public class Municipio : NamedMetadata
{
    public int ProvinciaId { get; set; }
    public virtual Provincia? Provincia { get; set; }
}