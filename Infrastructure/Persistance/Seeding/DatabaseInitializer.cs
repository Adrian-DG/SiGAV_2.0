using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistance.Seeding;

/// <summary>
/// Prepara la base al arrancar: esquema (opcional) y luego los seeders habilitados, en orden.
/// </summary>
internal sealed class DatabaseInitializer(
    SiGAVContext context,
    IEnumerable<ISeeder> seeders,
    IOptions<SeedingOptions> options,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var opciones = options.Value;

        if (opciones.ApplyMigrations)
            await PrepararEsquemaAsync(cancellationToken);

        if (!opciones.Enabled) return;

        var habilitados = seeders
            .Where(s => s.Categoria != SeedCategoria.Demo || opciones.DemoData)
            .OrderBy(s => s.Orden);

        foreach (var seeder in habilitados)
        {
            var nombre = seeder.GetType().Name;

            // Todo o nada por seeder: si falla a medias, en el próximo arranque sus tablas
            // siguen vacías y se vuelve a intentar.
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var insertados = await seeder.SeedAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            context.ChangeTracker.Clear();

            if (insertados > 0)
                logger.LogInformation("Seed {Seeder}: {Registros} registros insertados.", nombre, insertados);
            else
                logger.LogDebug("Seed {Seeder}: ya tenía datos, se omite.", nombre);
        }
    }

    private async Task PrepararEsquemaAsync(CancellationToken cancellationToken)
    {
        if (context.Database.GetMigrations().Any())
        {
            await context.Database.MigrateAsync(cancellationToken);
            return;
        }

        // Temporal hasta crear la primera migración: EnsureCreated no actualiza una base existente
        if (await context.Database.EnsureCreatedAsync(cancellationToken))
            logger.LogWarning("No hay migraciones: la base se creó a partir del modelo (EnsureCreated).");
    }
}
