using Domain.Abstraction;
using Domain.Entities.Misc;

namespace Domain.Entities.Historico;

public class Vehiculo : BaseEntityMetadata
{
    public int TipoId { get; set; }
    public virtual TipoVehiculo? Tipo { get; set; }
    
    public int ColorId { get; set; }
    public virtual Color? Color { get; set; }
    
    public int ModeloId { get; set; }
    public virtual Modelo? Modelo { get; set; }

    public int? Fabricacion { get; set; }
    
    public required string Placa { get; set; }

    public string? PlacaURI { get; set; }
    
}