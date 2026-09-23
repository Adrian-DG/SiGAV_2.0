using Domain.Abstraction;

namespace Domain.Entities.Misc;

public class Modelo : NamedMetadata
{
    public int MarcaId { get; set; }
    public Marca? Marca { get; set; }
    
    public virtual int TipoVehiculoId { get; set; }
    public virtual TipoVehiculo? TipoVehiculo { get; set; }
}