using Domain.Abstraction;
using Domain.Entities.Historico;
using Domain.Entities.Misc;

namespace Domain.Entities.Operaciones;

public class EventoCiudadano : IAuditableMetadata
{
    public int EventoId { get; set; }
    public virtual Evento? Evento { get; set; }
    
    public int CiudadanoId { get; set; }
    public virtual Ciudadano? Ciudadano { get; set; }

    public int VehiculoId { get; set; }
    public virtual Vehiculo? Vehiculo { get; set; }
    
    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}