using Domain.Abstraction;

namespace Domain.Entities.Misc;

/// <summary>
/// Centro de salud que recibe a los pacientes trasladados por las ambulancias (Hospital en SiGAV 1.0,
/// que lo agrupaba por RegionMacro; aquí se ubica por provincia, de la que se deriva la región).
/// </summary>
public class Hospital : NamedMetadata
{
    public const int NombreMaxLength = 150;

    /// <summary>Provincia donde está el centro (opcional: permite ofrecer primero los cercanos).</summary>
    public int? ProvinciaId { get; set; }
    public virtual Provincia? Provincia { get; set; }
}
