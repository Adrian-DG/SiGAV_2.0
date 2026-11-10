using Domain.Enums;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Región supervisada por una denominación de jerarquía Regional. Es una región macro
/// (cubre todas sus zonas, incluso las que se creen después) o una zona puntual; nunca ambas.
/// </summary>
public class DenominacionRegion
{
    public int Id { get; private set; }

    public int DenominacionId { get; private set; }

    public RegionMacroEnum? RegionMacro { get; private set; }

    public int? ZonaId { get; private set; }
    public virtual Zona? Zona { get; private set; }

    private DenominacionRegion() { }

    internal static DenominacionRegion DeMacro(RegionMacroEnum macro) => new() { RegionMacro = macro };

    internal static DenominacionRegion DeZona(int zonaId) => new() { ZonaId = zonaId };
}

/// <summary>
/// Tramo adicional supervisado por una denominación de jerarquía Tramo.
/// </summary>
public class DenominacionTramo
{
    public int DenominacionId { get; private set; }

    public int TramoId { get; private set; }
    public virtual Tramo? Tramo { get; private set; }

    private DenominacionTramo() { }

    internal DenominacionTramo(int tramoId) => TramoId = tramoId;
}
