using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Región supervisada por una denominación de jerarquía Regional. Es una región macro
/// (cubre todas sus regiones de asistencia, incluso las que se creen después) o una región
/// de asistencia puntual; nunca ambas.
/// </summary>
[Table("denominacion_regiones", Schema = "operaciones")]
public class DenominacionRegion
{
    public int Id { get; private set; }

    public int DenominacionId { get; private set; }

    public RegionMacroEnum? RegionMacro { get; private set; }

    [ForeignKey(nameof(RegionAsistencia))]
    public int? RegionAsistenciaId { get; private set; }
    public virtual RegionAsistencia? RegionAsistencia { get; private set; }

    private DenominacionRegion() { }

    internal static DenominacionRegion DeMacro(RegionMacroEnum macro) => new() { RegionMacro = macro };

    internal static DenominacionRegion DeAsistencia(int regionAsistenciaId) => new() { RegionAsistenciaId = regionAsistenciaId };
}

/// <summary>
/// Tramo adicional supervisado por una denominación de jerarquía Tramo.
/// </summary>
[Table("denominacion_tramos", Schema = "operaciones")]
public class DenominacionTramo
{
    public int DenominacionId { get; private set; }

    [ForeignKey(nameof(Tramo))]
    public int TramoId { get; private set; }
    public virtual Tramo? Tramo { get; private set; }

    private DenominacionTramo() { }

    internal DenominacionTramo(int tramoId) => TramoId = tramoId;
}
