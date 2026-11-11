using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistance;

/// <summary>
/// Configuración del proveedor compartida por la aplicación (DependencyInjection) y por las
/// herramientas de EF (<see cref="SiGAVContextDesignTimeFactory"/>): así las migraciones se
/// generan con las mismas opciones con las que corre la API.
/// </summary>
internal static class SiGAVContextOptions
{
    public static DbContextOptionsBuilder UsarSqlServer(this DbContextOptionsBuilder options, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Falta la cadena de conexión (ConnectionStrings:DevelopmentConnection / ProductionConnection).");

        // Sin EnableRetryOnFailure: la estrategia de reintentos no admite transacciones iniciadas por
        // el código (DatabaseInitializer abre una por seeder) sin envolverlas en CreateExecutionStrategy.
        return options.UseSqlServer(connectionString, b => b
            .MigrationsAssembly(typeof(SiGAVContext).Assembly.GetName().Name)
            // Colecciones en consultas separadas (evita el producto cartesiano de los Include)
            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }

    public static DbContextOptionsBuilder UsarSqlite(this DbContextOptionsBuilder options, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Falta la cadena de conexión (ConnectionStrings:DevelopmentConnection / ProductionConnection).");

        return options.UseSqlite(connectionString, b => b
            .MigrationsAssembly(typeof(SiGAVContext).Assembly.GetName().Name))
            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }
}
