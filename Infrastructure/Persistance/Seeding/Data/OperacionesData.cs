using Domain.Enums;

namespace Infrastructure.Persistance.Seeding.Data;

/// <summary>
/// Estructura operativa de SiGAV 1.0 (regiones de asistencia, tramos, tipos de unidad y de
/// asistencia), en el orden de sus Ids.
/// </summary>
internal static class OperacionesData
{
    /// <summary>
    /// El legacy no guardaba la región macro de cada región de asistencia; se asigna según la
    /// región macro de las provincias que cubre.
    /// </summary>
    public static readonly (string Nombre, RegionMacroEnum Macro)[] Regiones =
    [
        ("Region Este", RegionMacroEnum.ESTE),
        ("Region Las Americas", RegionMacroEnum.ESTE),
        ("Region del Nordeste", RegionMacroEnum.NORTE),
        ("Region Cibao Sur", RegionMacroEnum.NORTE),
        ("Region Noroeste", RegionMacroEnum.NORTE),
        ("Region Cibao Norte", RegionMacroEnum.NORTE),
        ("Region Sureste", RegionMacroEnum.SUR),
        ("Region Suroeste", RegionMacroEnum.SUR),
        ("Region Circunvalacion de Santo Domingo", RegionMacroEnum.ESTE)
    ];

    /// <summary>(Tramo, región de asistencia).</summary>
    public static readonly (string Nombre, string Region)[] Tramos =
    [
        ("Tramo Carretero Miches", "Region Este"),
        ("Tramo Carretero Punta Cana", "Region Este"),
        ("Tramo El Coral y Circ. Romana", "Region Este"),
        ("9-1-1 Romana", "Region Este"),
        ("Tramo del Seibo", "Region Este"),
        ("Las Américas tramo I", "Region Las Americas"),
        ("Las Américas tramo II", "Region Las Americas"),
        ("Tramo Hato Mayor", "Region Las Americas"),
        ("Tramo Samaná", "Region del Nordeste"),
        ("Cibao Sur tramo I", "Region Cibao Sur"),
        ("Tramo San Francisco de Macorís", "Region Cibao Sur"),
        ("Cibao Sur Tramo II", "Region Cibao Sur"),
        ("Tramo Cotuí", "Region Cibao Sur"),
        ("Tramo Salcedo", "Region Cibao Sur"),
        ("Tramo Circunvalación Norte", "Region Noroeste"),
        ("Noroeste tramo I", "Region Noroeste"),
        ("Noroeste tramo II", "Region Noroeste"),
        ("Tramo Mao, Valverde", "Region Noroeste"),
        ("Tramo Cibao Norte", "Region Cibao Norte"),
        ("Atlántico tramo I", "Region Cibao Norte"),
        ("Tramo Luperón", "Region Cibao Norte"),
        ("Atlántico tramo II", "Region Cibao Norte"),
        ("Tramo Rio San Juan", "Region Cibao Norte"),
        ("Sureste tramo I", "Region Sureste"),
        ("Sureste tramo II", "Region Sureste"),
        ("Tramo San Juan de la Maguana", "Region Sureste"),
        ("Tramo San José de Ocoa", "Region Sureste"),
        ("Suroeste tramo I", "Region Suroeste"),
        ("Suroeste tramo II", "Region Suroeste"),
        ("Circunvalación Santo Domingo tramo I", "Region Circunvalacion de Santo Domingo"),
        ("Circunvalación Santo Domingo tramo II", "Region Circunvalacion de Santo Domingo"),
        ("Corredores del Distrito Nacional", "Region Circunvalacion de Santo Domingo")
    ];

    /// <summary>Niveles de denominación (TipoUnidad en SiGAV 1.0).</summary>
    public static readonly (string Nombre, JerarquiaEnum Jerarquia, bool EsAmbulancia)[] NivelesDenominacion =
    [
        ("Supervisor Regional", JerarquiaEnum.Regional, false),
        ("Encargado de Tramo", JerarquiaEnum.Tramo, false),
        ("Móvil", JerarquiaEnum.Unidad, false),
        ("Unidad", JerarquiaEnum.Unidad, false),
        ("Taller", JerarquiaEnum.Unidad, false),
        ("Ambulancia", JerarquiaEnum.Unidad, true),
        ("Grúa", JerarquiaEnum.Unidad, false),
        ("Rescate", JerarquiaEnum.Unidad, false),
        ("CODEVIAL", JerarquiaEnum.Unidad, false),
        ("Motorizada", JerarquiaEnum.Unidad, false)
    ];

    /// <summary>Tipos de evento (TipoAsistencia en SiGAV 1.0).</summary>
    public static readonly (string Nombre, CategoriaEventoEnum Categoria)[] TiposEvento =
    [
        ("Choque", CategoriaEventoEnum.ACCIDENTE),
        ("Choque Multiple", CategoriaEventoEnum.ACCIDENTE),
        ("Choque con animal", CategoriaEventoEnum.ACCIDENTE),
        ("Deslizamiento", CategoriaEventoEnum.ACCIDENTE),
        ("Volcadura", CategoriaEventoEnum.ACCIDENTE),
        ("Atropellamiento", CategoriaEventoEnum.ACCIDENTE),
        ("Seguridad", CategoriaEventoEnum.ASISTENCIA),
        ("Neumático", CategoriaEventoEnum.ASISTENCIA),
        ("Combustible", CategoriaEventoEnum.ASISTENCIA),
        ("Mecanica", CategoriaEventoEnum.ASISTENCIA),
        ("Electrica", CategoriaEventoEnum.ASISTENCIA),
        ("Calentamiento", CategoriaEventoEnum.ASISTENCIA),
        ("Grúas", CategoriaEventoEnum.ASISTENCIA),
        ("Ambulancia", CategoriaEventoEnum.ASISTENCIA),
        ("Talleres", CategoriaEventoEnum.ASISTENCIA),
        ("Camión. Rescate", CategoriaEventoEnum.ASISTENCIA)
    ];
}
