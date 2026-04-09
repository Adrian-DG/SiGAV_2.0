using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Contracts.Authentication;
using Infrastructure.Repositories.Authentication;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IJwtBearerHelper, JwtBearerHelper>();
        services.AddScoped<IAuthRepository, AuthRepository>();
    }
}