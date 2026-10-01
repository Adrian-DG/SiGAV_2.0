namespace Infrastructure.Persistance.Seeding.Data;

/// <summary>Catálogos de vehículos (tomados de SiGAV 1.0, en el orden de sus Ids).</summary>
internal static class VehiculosData
{
    public static readonly string[] TiposVehiculo =
    [
        "Desconocido",
        "Autobus",
        "Camión",
        "Camioneta",
        "Carro",
        "Jeepeta",
        "Jeep",
        "Guagua",
        "Motor o Motocicleta",
        "Patana"
    ];

    /// <summary>
    /// (Prefijo, descripción, patrón, ejemplo, tipos de vehículo). Prefijos según la DGII; los
    /// formatos (cantidad de dígitos) deben confirmarse con la lista oficial y se corrigen en el
    /// catálogo sin publicar la app. Sin tipos = cualquier tipo.
    /// </summary>
    public static readonly (string Prefijo, string Nombre, string Patron, string Ejemplo, string[] Tipos)[] PrefijosPlaca =
    [
        ("A", "Automóvil privado", @"^A\d{5,6}$", "A123456", ["Carro"]),
        ("G", "Jeep privado", @"^G\d{5,6}$", "G123456", ["Jeep", "Jeepeta"]),
        ("L", "Carga", @"^L\d{5,6}$", "L123456", ["Camión", "Camioneta", "Patana"]),
        ("K", "Motocicleta", @"^K\d{5,6}$", "K123456", ["Motor o Motocicleta"]),
        ("N", "Motocicleta (placa anterior a la K)", @"^N\d{5,6}$", "N123456", ["Motor o Motocicleta"]),
        ("X", "Exhibición", @"^X\d{5,6}$", "X123456", []),
        ("OE", "Ejército", @"^OE\d{4,6}$", "OE12345", [])
    ];

    public static readonly string[] Colores =
    [
        "Otro",
        "Amarillo",
        "Azul",
        "Blanco",
        "Crema",
        "Gris",
        "Gris oscuro",
        "Marron",
        "Naranja",
        "Negro",
        "Rojo",
        "Rojo Vino",
        "Verde",
        "Morado"
    ];

    public static readonly string[] Marcas =
    [
        "Desconocida",
        "Acura",
        "Audi",
        "BMW",
        "Chevrolet",
        "Daihatsu",
        "Ford",
        "Hyundai",
        "Honda",
        "Infiniti",
        "Isuzu",
        "Jeep",
        "Kia",
        "Lexus",
        "Mazda",
        "Mercedes Benz",
        "Nissan",
        "Otro",
        "Scion",
        "Skoda",
        "Toyota",
        "Volkswagen",
        "Volvo",
        "Subaru"
    ];

    /// <summary>(Modelo, marca, tipo de vehículo).</summary>
    public static readonly (string Nombre, string Marca, string Tipo)[] Modelos =
    [
        ("Camry", "Toyota", "Carro"),
        ("Corrolla", "Toyota", "Carro"),
        ("Highlander", "Toyota", "Jeepeta"),
        ("LandCruiser", "Toyota", "Jeepeta"),
        ("Hilux", "Toyota", "Camioneta"),
        ("Tacoma", "Toyota", "Camioneta"),
        ("Tundra", "Toyota", "Camioneta"),
        ("Sequoia", "Toyota", "Jeepeta"),
        ("Civic", "Honda", "Carro"),
        ("CR-V", "Honda", "Jeepeta"),
        ("Fit", "Honda", "Carro"),
        ("HR-V", "Honda", "Jeepeta"),
        ("Pilot", "Honda", "Jeepeta"),
        ("Accord", "Honda", "Carro"),
        ("Frontier", "Nissan", "Camioneta"),
        ("Sentra", "Nissan", "Carro"),
        ("Tiida", "Nissan", "Carro"),
        ("Pathfinder", "Nissan", "Jeepeta"),
        ("Kicks", "Nissan", "Jeepeta"),
        ("Titan", "Nissan", "Camioneta"),
        ("Mazda 3", "Mazda", "Carro"),
        ("Mazda 6", "Mazda", "Carro"),
        ("CX-5", "Mazda", "Jeepeta"),
        ("CX-9", "Mazda", "Jeepeta"),
        ("Mazda 2 (Demio)", "Mazda", "Carro"),
        ("Accent", "Hyundai", "Carro"),
        ("Sonata", "Hyundai", "Carro"),
        ("Tucson", "Hyundai", "Jeepeta"),
        ("Santa Fe", "Hyundai", "Jeepeta"),
        ("Elantra", "Hyundai", "Carro"),
        ("Y20", "Hyundai", "Carro"),
        ("i10", "Hyundai", "Carro"),
        ("i20", "Hyundai", "Carro"),
        ("K5", "Kia", "Carro"),
        ("Forte", "Kia", "Carro"),
        ("Sorento", "Kia", "Jeepeta"),
        ("Sportage", "Kia", "Jeepeta"),
        ("Rio", "Kia", "Carro"),
        ("Mira", "Daihatsu", "Carro"),
        ("Sirion", "Daihatsu", "Carro"),
        ("Camion", "Daihatsu", "Camión"),
        ("Terios", "Daihatsu", "Jeepeta"),
        ("Escape", "Ford", "Jeepeta"),
        ("F-150", "Ford", "Camioneta"),
        ("Focus", "Ford", "Carro"),
        ("Explorer", "Ford", "Jeepeta"),
        ("Ranger", "Ford", "Camioneta"),
        ("Ecosport", "Ford", "Jeepeta"),
        ("Wrangler", "Jeep", "Jeep"),
        ("Cherokee", "Jeep", "Jeepeta"),
        ("Grand Cherokee", "Jeep", "Jeepeta"),
        ("Cruize", "Chevrolet", "Carro"),
        ("Aveo", "Chevrolet", "Carro"),
        ("Colorado", "Chevrolet", "Camioneta"),
        ("Silverado", "Chevrolet", "Camioneta"),
        ("Trax", "Chevrolet", "Jeepeta"),
        ("Traverse", "Chevrolet", "Jeepeta"),
        ("Otro", "Desconocida", "Desconocido"),
        ("Touareg", "Volkswagen", "Jeepeta"),
        ("Tiguan", "Volkswagen", "Jeepeta"),
        ("Jetta", "Volkswagen", "Carro"),
        ("Passat", "Volkswagen", "Carro"),
        ("Golf", "Volkswagen", "Carro"),
        ("Fox", "Volkswagen", "Carro"),
        ("D-MAX", "Isuzu", "Camioneta"),
        ("Rodeo", "Isuzu", "Jeepeta"),
        ("MUX", "Isuzu", "Jeepeta"),
        ("XC-60", "Volvo", "Jeepeta"),
        ("XC-90", "Volvo", "Jeepeta"),
        ("XC-40", "Volvo", "Jeepeta"),
        ("Legacy", "Subaru", "Carro"),
        ("Impreza", "Subaru", "Carro"),
        ("Outback", "Subaru", "Jeepeta"),
        ("Forester", "Subaru", "Jeepeta"),
        ("XV (Crosstrek)", "Subaru", "Jeepeta")
    ];
}
