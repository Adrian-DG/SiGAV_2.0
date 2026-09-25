using Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Application.Contracts.Authentication;
using Domain.Repositories;
using Infrastructure.Authentication;
using Infrastructure.Helpers;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Infrastructure.Persistance;
using Infrastructure.Persistance.Authentication;
using Infrastructure.Persistance.Historico;
using Infrastructure.Persistance.Interceptors;
using Infrastructure.Persistance.Operaciones;
using Infrastructure.Persistance.Seeding;
using Infrastructure.Services;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<SiGAVContext>((sp, opt) =>
        {
            var connectionString = environment.IsProduction()
                ? configuration.GetConnectionString("ProductionConnection")
                : configuration.GetConnectionString("DevelopmentConnection");

            opt.UseSqlite(connectionString, b => b
                .MigrationsAssembly("Infrastructure")
                // Colecciones en consultas separadas (evita el producto cartesiano de los Include)
                .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            opt.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            opt.EnableDetailedErrors();
        });
        // Mismo contexto por request: escritura (IUnitOfWork + repositorios) y lectura (Queries)
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SiGAVContext>());
        services.AddScoped<IReadDbContext>(sp => sp.GetRequiredService<SiGAVContext>());

        services.AddIdentityCore<AppUser>()
            .AddRoles<AppPermission>()
            .AddEntityFrameworkStores<SiGAVContext>();

        services.AddJwtAuthentication(configuration);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IJwtBearerHelper, JwtBearerHelper>();
        services.AddScoped<IAuthRepository, AuthRepository>();

        // Repositorios del dominio (lado de escritura). La lectura la hacen las Queries con IReadDbContext.
        services.AddScoped<IUnidadRepository, UnidadRepository>();
        services.AddScoped<IDenominacionRepository, DenominacionRepository>();
        services.AddScoped<IAgenteRepository, AgenteRepository>();
        services.AddScoped<IEventoRepository, EventoRepository>();
        services.AddScoped<IHistoricoRepository, HistoricoRepository>();

        // Carga inicial (catálogos, administrador y datos de prueba) al arrancar
        services.AddSeeding(configuration);

        return services;
    }
}
