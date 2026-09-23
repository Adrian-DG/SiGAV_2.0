using Domain.Abstraction;
using Domain.Entities.Misc;
using Domain.Enums;

namespace Domain.Entities.Operaciones;

public class Agente : PersonMetadata, IAuditableMetadata
{
    public InstitucionEnum Institucion { get; set; }
    
    public int RangoId { get; set; }
    public virtual Rango? Rango { get; set; }
    
    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // La Armada (ARD) usa su propia nomenclatura de rangos. Requiere que Rango esté cargado.
    public string GetRango => (Institucion == InstitucionEnum.ARD ? Rango?.NombreArmada : Rango?.Nombre) ?? string.Empty;
    
    public string GetInfo => $"{GetRango}, {Apellido} {Nombre}, {Institucion.ToString()}";
}