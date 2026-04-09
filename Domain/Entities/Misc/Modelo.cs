using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Misc;

[Table("modelos", Schema = "misc")]
public class Modelo : NamedMetadata
{
    [ForeignKey(nameof(Marca))]
    public int MarcaId { get; set; }
    public Marca? Marca { get; set; }
    
    [ForeignKey(nameof(TipoVehiculo))]
    public virtual int TipoVehiculoId { get; set; }
    public virtual TipoVehiculo? TipoVehiculo { get; set; }
}