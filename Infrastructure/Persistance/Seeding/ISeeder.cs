namespace Infrastructure.Persistance.Seeding;

public enum SeedCategoria
{
    /// <summary>Datos de referencia que el sistema necesita en todo ambiente (provincias, rangos, tramos...).</summary>
    Catalogo = 1,

    /// <summary>Usuario administrador inicial de la web.</summary>
    Identidad = 2,

    /// <summary>Datos de prueba (denominaciones, unidades, agentes...). Nunca en producción.</summary>
    Demo = 3
}

/// <summary>
/// Carga inicial de un grupo de tablas. Cada seeder es idempotente: solo inserta si sus tablas
/// están vacías, así que puede ejecutarse en cada arranque. <see cref="DatabaseInitializer"/>
/// ejecuta cada uno en su propia transacción, en el orden de <see cref="Orden"/>.
/// </summary>
internal interface ISeeder
{
    SeedCategoria Categoria { get; }

    /// <summary>Los catálogos van primero porque los demás seeders los buscan por nombre.</summary>
    int Orden { get; }

    /// <returns>Cantidad de registros insertados (0 si ya había datos).</returns>
    Task<int> SeedAsync(CancellationToken cancellationToken);
}
