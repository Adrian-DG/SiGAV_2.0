namespace Infrastructure.Persistance.Seeding.Data;

/// <summary>Catálogos del personal (agentes y usuarios de la web).</summary>
internal static class PersonalData
{
    /// <summary>
    /// Rangos de SiGAV 1.0, en el orden de sus Ids. El legacy guardaba "Ejército/Armada" en un
    /// solo texto; aquí cada nomenclatura va en su columna (la Armada usa <c>NombreArmada</c>).
    /// </summary>
    public static readonly (string Nombre, string NombreArmada)[] Rangos =
    [
        ("MAYOR GENERAL", "VICEALMIRANTE"),
        ("GENERAL", "CONTRALMIRANTE"),
        ("CORONEL", "CAPITAN DE NAVIO"),
        ("TENIENTE CORONEL", "CAPITAN DE FRAGATA"),
        ("MAYOR", "CAPITAN DE CORBETA"),
        ("CAPITAN", "TENIENTE DE NAVIO"),
        ("1ER TENIENTE", "TENIENTE DE FRAGATA"),
        ("2DO TENIENTE", "TENIENTE DE CORBETA"),
        ("SARGENTO MAYOR", "SARGENTO MAYOR"),
        ("SARGENTO", "SARGENTO"),
        ("CABO", "CABO"),
        ("RASO", "MARINERO"),
        ("ASIMILADO", "ASIMILADO"),
        ("AGENTE MOPC", "AGENTE MOPC")
    ];

    public static readonly string[] Departamentos =
    [
        "Centro de Operaciones S3",
        "Call Center R5"
    ];

    public static readonly string[] Nacionalidades =
    [
        "Dominicana",
        "Haitiana",
        "Venezolana",
        "Estadounidense",
        "Colombiana",
        "Cubana",
        "Española",
        "Otra"
    ];
}
