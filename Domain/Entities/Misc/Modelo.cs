using Domain.Abstraction;

namespace Domain.Entities.Misc;

public class Modelo : NamedMetadata
{
    public int MarcaId { get; set; }
    public virtual Marca? Marca { get; set; }
    
    public int TipoVehiculoId { get; set; }
    public virtual TipoVehiculo? TipoVehiculo { get; set; }
}