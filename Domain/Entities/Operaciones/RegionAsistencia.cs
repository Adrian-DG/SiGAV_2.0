using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities.Operaciones;

[Table("regiones_asistencia", Schema = "operaciones")]
public class RegionAsistencia : NamedMetadata
{
    public RegionMacroEnum Region { get; set; }
}