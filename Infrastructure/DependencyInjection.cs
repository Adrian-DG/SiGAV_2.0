using Application.Contracts;
using Application.Contracts.Historico;
using Application.Contracts.Operaciones;
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
using Infrastructure.Persistance.Misc;
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

            opt.UseSqlite(connectionString,  b => b.MigrationsAssembly("Infrastructure"));
            opt.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            opt.EnableDetailedErrors();
        });
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SiGAVContext>());

        services.AddIdentityCore<AppUser>()
            .AddRoles<AppPermission>()
            .AddEntityFrameworkStores<SiGAVContext>();

        services.AddJwtAuthentication(configuration);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IJwtBearerHelper, JwtBearerHelper>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IMiscRepository, MiscQueryService>();

        // Operaciones: escritura (repositorios del dominio) y lectura (query services)
        services.AddScoped<IUnidadRepository, UnidadRepository>();
        services.AddScoped<IDenominacionRepository, DenominacionRepository>();
        services.AddScoped<IAgenteRepository, AgenteRepository>();
        services.AddScoped<IAgenteQueries, AgenteQueries>();
        services.AddScoped<IEventoRepository, EventoRepository>();
        services.AddScoped<IEventoQueries, EventoQueries>();
        services.AddScoped<IHistoricoRepository, HistoricoRepository>();
        services.AddScoped<IHistoricoQueries, HistoricoQueries>();
        services.AddScoped<IUnidadQueries, UnidadQueries>();
        services.AddScoped<IDenominacionQueries, DenominacionQueries>();
        services.AddScoped<ICatalogoQueries, CatalogoQueries>();
        services.AddScoped<IEstadisticasQueries, EstadisticasQueries>();
        services.AddScoped<IHistorialDenominacionQueries, HistorialDenominacionQueries>();

        // Carga inicial (catálogos, administrador y datos de prueba) al arrancar
        services.AddSeeding(configuration);

        return services;
    }
}
