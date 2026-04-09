using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;
using Domain.Entities.Misc;

namespace Domain.Entities.Historico;

[Table("ciudadanos", Schema = "historico")]
public class Ciudadano : PersonMetadata
{
    public string? FotoIdentificacion  { get; set; }
    
    [ForeignKey(nameof(Nacionalidad))]
    public int NacionalidadId { get; set; }
    public virtual Nacionalidad? Nacionalidad { get; set; }
}