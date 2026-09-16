using Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Contracts.Authentication;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Infrastructure.Persistance;
using Infrastructure.Persistance.Authentication;
using Infrastructure.Persistance.Misc;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddDbContext<SiGAVContext>(opt =>
        {
            var connectionString = environment.IsProduction()
                ? configuration.GetConnectionString("ProductionConnection")
                : configuration.GetConnectionString("DevelopmentConnection");
            
            opt.UseSqlite(connectionString,  b => b.MigrationsAssembly("Infrastructure"));
            opt.EnableDetailedErrors();
        });

        services.AddSingleton<IJwtBearerHelper, JwtBearerHelper>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IMiscRepository, MiscQueryService>();

        return services;
    }
}