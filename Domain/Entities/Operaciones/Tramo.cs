using Domain.Abstraction;

namespace Domain.Entities.Operaciones;

public class Tramo : NamedMetadata
{
    public int RegionAsistenciaId  { get; set; }
    public RegionAsistencia? RegionAsistencia { get; set; }
}