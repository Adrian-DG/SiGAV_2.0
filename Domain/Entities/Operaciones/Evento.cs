using Domain.Abstraction;
using Domain.Entities.Historico;
using Domain.Entities.Misc;

namespace Domain.Entities.Operaciones;

public class Evento : BaseEntityMetadata, IAuditableMetadata
{
    public virtual ICollection<EventoCiudadano>? Ciudadanos { get; set; }
    
    public virtual ICollection<EventoUnidad>? Unidades { get; set; }

    public virtual ICollection<EventoTipoEvento>? Tipos { get; set; }
    
    public required string Coordenadas { get; set; }

    public string? UbicacionText { get; set; }
    
    public int MunicipioId  { get; set; }
    public virtual Municipio? Municipio { get; set; }

    public string? Comentario { get; set; }
    
    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}