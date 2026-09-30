using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistance;

/// <summary>
/// Solo para las herramientas de EF (dotnet ef migrations / database update). Permite ejecutarlas
/// con <c>--project Infrastructure</c> sin levantar la API (ni sus seeders).
/// La cadena de conexión se toma del appsettings de Presentation o de la variable de entorno
/// <c>ConnectionStrings__DevelopmentConnection</c>.
/// </summary>
internal sealed class SiGAVContextDesignTimeFactory : IDesignTimeDbContextFactory<SiGAVContext>
{
    public SiGAVContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var presentation = BuscarPresentation();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(presentation)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = environment == "Production"
            ? configuration.GetConnectionString("ProductionConnection")
            : configuration.GetConnectionString("DevelopmentConnection");

        // Una ruta relativa se resuelve desde Presentation: la misma base que usa la API al correr
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var sqlite = new SqliteConnectionStringBuilder(connectionString);
            if (!Path.IsPathRooted(sqlite.DataSource) && sqlite.DataSource != ":memory:")
                sqlite.DataSource = Path.Combine(presentation, sqlite.DataSource);
            connectionString = sqlite.ToString();
        }

        // Sin interceptores: la auditoría no aplica al generar o aplicar migraciones
        var options = new DbContextOptionsBuilder<SiGAVContext>();
        options.UsarSqlite(connectionString);
        return new SiGAVContext(options.Options);
    }

    /// <summary>Carpeta de Presentation, desde la carpeta de Infrastructure o la de la solución.</summary>
    private static string BuscarPresentation()
    {
        var actual = Directory.GetCurrentDirectory();
        string[] candidatas = [Path.Combine(actual, "..", "Presentation"), Path.Combine(actual, "Presentation"), actual];
        return Path.GetFullPath(candidatas.FirstOrDefault(c => File.Exists(Path.Combine(c, "appsettings.json"))) ?? actual);
    }
}
