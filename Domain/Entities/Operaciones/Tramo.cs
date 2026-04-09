using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;

namespace Domain.Entities.Operaciones;

[Table("tramos", Schema = "operaciones")]
public class Tramo : NamedMetadata
{
    [ForeignKey(nameof(RegionAsistencia))]
    public int RegionAsistenciaId  { get; set; }
    public RegionAsistencia? RegionAsistencia { get; set; }
}