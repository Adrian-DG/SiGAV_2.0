using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;
using Domain.Entities.Misc;

namespace Domain.Entities.Historico;

[Table("vehiculos", Schema = "historico")]
public class Vehiculo : BaseEntityMetadata
{
    [ForeignKey(nameof(Tipo))]
    public int TipoId { get; set; }
    public virtual TipoVehiculo? Tipo { get; set; }
    
    [ForeignKey(nameof(Color))]
    public int ColorId { get; set; }
    public virtual Color? Color { get; set; }
    
    [ForeignKey(nameof(Modelo))]
    public int ModeloId { get; set; }
    public virtual Modelo? Modelo { get; set; }

    public int? Fabricacion { get; set; }
    
    public required string Placa { get; set; }

    public string? PlacaURI { get; set; }
    
}