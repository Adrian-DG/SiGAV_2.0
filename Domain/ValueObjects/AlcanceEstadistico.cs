using Domain.Enums;

namespace Domain.ValueObjects;

/// <summary>
/// Conjunto de eventos que una sesión puede ver en las estadísticas, según su jerarquía.
/// </summary>
public sealed record AlcanceEstadistico
{
    /// <summary>null = sin restricción (usuario web / front desk).</summary>
    public JerarquiaEnum? Jerarquia { get; private init; }
    public int? DenominacionId { get; private init; }
    public int? UnidadId { get; private init; }
    public IReadOnlyCollection<RegionMacroEnum> RegionesMacro { get; private init; } = [];
    public IReadOnlyCollection<int> RegionesAsistencia { get; private init; } = [];
    public IReadOnlyCollection<int> Tramos { get; private init; } = [];

    public bool EsGlobal => Jerarquia is null;

    /// <summary>Una denominación regional o de tramo sin asignaciones no ve nada.</summary>
    public bool EstaVacio => Jerarquia switch
    {
        JerarquiaEnum.Regional => RegionesMacro.Count == 0 && RegionesAsistencia.Count == 0,
        JerarquiaEnum.Tramo => Tramos.Count == 0,
        JerarquiaEnum.Unidad => UnidadId is null,
        _ => false
    };

    private AlcanceEstadistico() { }

    public static AlcanceEstadistico Global() => new();

    public static AlcanceEstadistico Regional(int denominacionId, IEnumerable<RegionMacroEnum> macros, IEnumerable<int> regionesAsistencia) => new()
    {
        Jerarquia = JerarquiaEnum.Regional,
        DenominacionId = denominacionId,
        RegionesMacro = macros.Distinct().ToArray(),
        RegionesAsistencia = regionesAsistencia.Distinct().ToArray()
    };

    public static AlcanceEstadistico DeTramos(int denominacionId, IEnumerable<int> tramos) => new()
    {
        Jerarquia = JerarquiaEnum.Tramo,
        DenominacionId = denominacionId,
        Tramos = tramos.Distinct().ToArray()
    };

    public static AlcanceEstadistico DeUnidad(int denominacionId, int unidadId) => new()
    {
        Jerarquia = JerarquiaEnum.Unidad,
        DenominacionId = denominacionId,
        UnidadId = unidadId
    };
}
