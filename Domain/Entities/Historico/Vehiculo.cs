using Domain.Abstraction;
using Domain.Entities.Misc;
using System.Text.RegularExpressions;

namespace Domain.Entities.Historico;

public class Vehiculo : BaseEntityMetadata
{
    public int TipoId { get; set; }
    public virtual TipoVehiculo? Tipo { get; set; }
    
    public int ColorId { get; set; }
    public virtual Color? Color { get; set; }
    
    // Opcional: el modelo puede no estar en el catálogo (el texto libre queda en el evento)
    public int? ModeloId { get; set; }
    public virtual Modelo? Modelo { get; set; }

    public int? Fabricacion { get; set; }
    
    public required string Placa { get; set; }

}