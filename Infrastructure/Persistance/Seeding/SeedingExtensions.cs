using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistance.Seeding;

public static class SeedingExtensions
{
    /// <summary>Registra el inicializador y todos los <see cref="ISeeder"/> del ensamblado.</summary>
    internal static IServiceCollection AddSeeding(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SeedingOptions>(configuration.GetSection(SeedingOptions.SectionName));
        services.AddScoped<DatabaseInitializer>();

        var seeders = typeof(ISeeder).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ISeeder).IsAssignableFrom(t));

        foreach (var seeder in seeders)
            services.AddScoped(typeof(ISeeder), seeder);

        return services;
    }

    /// <summary>Prepara la base y ejecuta los seeders habilitados. Llamar antes de <c>app.Run()</c>.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);
    }
}
