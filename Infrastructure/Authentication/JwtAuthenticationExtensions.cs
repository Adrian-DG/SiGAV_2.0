using Application.Contracts.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Un único esquema JwtBearer valida los dos tipos de token (misma llave y emisor,
    /// audiencias distintas). Las políticas exigen el tipo de sesión y la audiencia que
    /// le corresponde, así que un token móvil no sirve en endpoints web ni viceversa.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Conserva los nombres de claim del token ("sub", "permission"...) sin traducirlos a ClaimTypes
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudiences = [jwt.Web.Audience, jwt.Movil.Audience],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwt.GetSigningKeyBytes()),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
                    NameClaimType = SesionClaims.Name,
                    RoleClaimType = SesionClaims.Permiso
                };

                bearer.Events = new JwtBearerEvents
                {
                    // Mismo formato { message } que el resto de errores de la API
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            message = context.AuthenticateFailure is SecurityTokenExpiredException
                                ? "La sesión expiró, inicie sesión nuevamente."
                                : "Se requiere una sesión válida."
                        });
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            message = "La sesión actual no tiene permiso para esta operación."
                        });
                    }
                };
            });

        services.AddAuthorization();
        services.AddOptions<AuthorizationOptions>()
            .Configure<IOptions<JwtOptions>>((authorization, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                authorization.AddPolicy(SesionPolicies.Web, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(SesionClaims.TipoSesion, TiposSesion.Web)
                    .RequireClaim(SesionClaims.Audience, jwt.Web.Audience));

                authorization.AddPolicy(SesionPolicies.Movil, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(SesionClaims.TipoSesion, TiposSesion.Movil)
                    .RequireClaim(SesionClaims.Audience, jwt.Movil.Audience));

                authorization.AddPolicy(SesionPolicies.Operativa, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        (context.User.HasClaim(SesionClaims.TipoSesion, TiposSesion.Web)
                            && context.User.HasClaim(SesionClaims.Audience, jwt.Web.Audience))
                        || (context.User.HasClaim(SesionClaims.TipoSesion, TiposSesion.Movil)
                            && context.User.HasClaim(SesionClaims.Audience, jwt.Movil.Audience))));
            });

        return services;
    }
}
