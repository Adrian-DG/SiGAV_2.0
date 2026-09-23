using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities.Operaciones;

public class RegionAsistencia : NamedMetadata
{
    public RegionMacroEnum Region { get; set; }
}