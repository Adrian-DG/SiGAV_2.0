using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;
using Domain.Entities.Misc;
using Domain.Enums;

namespace Domain.Entities.Operaciones;

[Table("agentes", Schema = "operaciones")]
public class Agente : PersonMetadata, IAuditableMetadata
{
    public InstitucionEnum Institucion { get; set; }
    
    [ForeignKey(nameof(Rango))] 
    public int RangoId { get; set; }
    public virtual Rango? Rango { get; set; }
    
    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    public string GetRango => Institucion == InstitucionEnum.ARD ? Rango.NombreArmada : Rango.NombreArmada;
    
    public string GetInfo => $"{GetRango}, {Apellido} {Nombre}, {Institucion.ToString()}";
}