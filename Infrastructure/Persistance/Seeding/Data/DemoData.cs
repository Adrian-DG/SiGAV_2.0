using Domain.Enums;

namespace Infrastructure.Persistance.Seeding.Data;

/// <summary>
/// Datos de prueba para desarrollo: una estructura operativa pequeña que cubre las tres
/// jerarquías (regional, tramo y unidad), agentes de varias instituciones y maestros para el
/// autocompletado de ciudadanos y vehículos. Las cédulas y placas son ficticias.
/// </summary>
internal static class DemoData
{
    public sealed record DenominacionDemo(
        string Nombre,
        string Nivel,
        string Tramo,
        string Ficha,
        string? Placa = null,
        RegionMacroEnum[]? RegionesMacro = null,
        string[]? RegionesAsistencia = null,
        string[]? TramosAdicionales = null);

    public static readonly DenominacionDemo[] Denominaciones =
    [
        // Regionales: una por región macro y otra por regiones de asistencia puntuales
        new("Supervisor Norte", "Supervisor Regional", "Tramo Cibao Norte", "S-01", "EL00101",
            RegionesMacro: [RegionMacroEnum.NORTE]),
        new("Supervisor Este", "Supervisor Regional", "Tramo Carretero Punta Cana", "S-02", "EL00102",
            RegionesAsistencia: ["Region Este", "Region Las Americas"]),

        // Encargados de tramo: su tramo más los adicionales
        new("Encargado Punta Cana", "Encargado de Tramo", "Tramo Carretero Punta Cana", "E-01", "EL00201",
            TramosAdicionales: ["Tramo Carretero Miches", "Tramo del Seibo"]),
        new("Encargado Las Américas", "Encargado de Tramo", "Las Américas tramo I", "E-02", "EL00202",
            TramosAdicionales: ["Las Américas tramo II"]),

        // Unidades operativas
        new("Móvil 101", "Móvil", "Tramo Carretero Punta Cana", "M-101", "EL00301"),
        new("Móvil 102", "Móvil", "Tramo Carretero Miches", "M-102", "EL00302"),
        new("Móvil 201", "Móvil", "Las Américas tramo I", "M-201", "EL00303"),
        new("Motorizada 202", "Motorizada", "Las Américas tramo II", "MT-202"),
        new("Ambulancia 301", "Ambulancia", "Tramo El Coral y Circ. Romana", "A-301", "EL00401"),
        new("Grúa 302", "Grúa", "Tramo El Coral y Circ. Romana", "G-302", "EL00402"),
        new("Móvil 401", "Móvil", "Tramo Cibao Norte", "M-401", "EL00304")
    ];

    /// <summary>Unidades sin denominación (en reserva), para probar asignaciones y reasignaciones.</summary>
    public static readonly (string Ficha, string? Placa)[] UnidadesEnReserva =
    [
        ("M-900", "EL00901"),
        ("M-901", null)
    ];

    public sealed record AgenteDemo(
        string Identificacion,
        string Nombre,
        string Apellido,
        SexoEnum Sexo,
        InstitucionEnum Institucion,
        string Rango,
        AreaOperativaEnum Area,
        bool AccesoTotal = false,
        string? Especialidad = null,
        bool Autorizado = true);

    public static readonly AgenteDemo[] Agentes =
    [
        // Mismo agente de las pruebas de la app móvil (cédula 001-1234567-8)
        new("00112345678", "Juan", "Pérez", SexoEnum.MASCULINO, InstitucionEnum.ARD, "SARGENTO", AreaOperativaEnum.AsistenciaVial),
        new("00100000002", "María", "Rodríguez", SexoEnum.FEMININO, InstitucionEnum.ERD, "CABO", AreaOperativaEnum.PreHospitalaria,
            Especialidad: "Paramédico"),
        new("00100000003", "Pedro", "Martínez", SexoEnum.MASCULINO, InstitucionEnum.PN, "1ER TENIENTE", AreaOperativaEnum.GestionOperativa,
            AccesoTotal: true),
        new("00100000004", "Carlos", "Santos", SexoEnum.MASCULINO, InstitucionEnum.MOPC, "AGENTE MOPC", AreaOperativaEnum.Gruas),
        new("00100000005", "Ana", "Féliz", SexoEnum.FEMININO, InstitucionEnum.FARD, "RASO", AreaOperativaEnum.Rescate),
        // Registrado desde la app, pendiente de autorización de front desk
        new("00100000006", "Luis", "Mena", SexoEnum.MASCULINO, InstitucionEnum.ERD, "SARGENTO MAYOR", AreaOperativaEnum.AsistenciaVial,
            Autorizado: false)
    ];

    public static readonly (string Identificacion, string Nombre, string Apellido, SexoEnum Sexo, string Nacionalidad)[] Ciudadanos =
    [
        ("40211112222", "Rosa", "Jiménez", SexoEnum.FEMININO, "Dominicana"),
        ("40233334444", "Miguel", "Castillo", SexoEnum.MASCULINO, "Dominicana"),
        ("AB1234567", "Jean", "Pierre", SexoEnum.MASCULINO, "Haitiana")
    ];

    /// <summary>(Placa, tipo, color, marca y modelo del catálogo, año).</summary>
    public static readonly (string Placa, string Tipo, string Color, string Marca, string Modelo, int? Fabricacion)[] Vehiculos =
    [
        ("G445566", "Jeepeta", "Blanco", "Toyota", "Highlander", 2019),
        ("A123456", "Carro", "Rojo", "Honda", "Civic", 2016),
        ("L987654", "Camioneta", "Gris", "Nissan", "Frontier", null)
    ];
}
