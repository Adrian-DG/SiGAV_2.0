using Domain.Abstraction;
using Domain.Entities.Misc;

namespace Domain.Entities.Historico;

public class Ciudadano : PersonMetadata
{
    public string? FotoIdentificacion  { get; set; }
    
    public int NacionalidadId { get; set; }
    public virtual Nacionalidad? Nacionalidad { get; set; }
}