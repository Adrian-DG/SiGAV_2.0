using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Misc;

[Table("municipios", Schema = "misc")]
public class Municipio : NamedMetadata
{
    [ForeignKey(nameof(Provincia))]
    public int ProvinciaId { get; set; }
    public virtual Provincia? Provincia { get; set; }
}